using System.Linq.Expressions;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Persistence.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private const int DefaultPageSize = 12;
    private const int MaximumPageSize = 100;

    private static readonly Expression<Func<Recipe, RecipeListItemDto>> ListProjection =
        recipe => new RecipeListItemDto(
            recipe.Id,
            recipe.Title,
            recipe.Slug,
            recipe.Description,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.Servings,
            recipe.Difficulty.ToString(),
            recipe.Images
                .Where(image => !image.IsDeleted)
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.OrderIndex)
                .ThenBy(image => image.Id)
                .Select(image => image.OriginalUrl)
                .FirstOrDefault(),
            new RecipeCategoryDto(
                recipe.Category.Id,
                recipe.Category.Name,
                recipe.Category.Slug,
                recipe.Category.Description),
            recipe.PublishedAt);

    private readonly ApplicationDbContext _db;

    public RecipeRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResultDto<RecipeListItemDto>> GetPublishedAsync(
        int page,
        int pageSize,
        RecipeListOptions options,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published);

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(recipe =>
                EF.Functions.ILike(recipe.Title, $"%{search}%") ||
                EF.Functions.ILike(recipe.Description, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(options.CategorySlug))
        {
            var categorySlug = options.CategorySlug.Trim();
            query = query.Where(recipe => recipe.Category.Slug == categorySlug);
        }

        if (options.Difficulty.HasValue)
        {
            query = query.Where(recipe => recipe.Difficulty == options.Difficulty.Value);
        }

        if (options.CategoryId.HasValue)
        {
            query = query.Where(recipe => recipe.CategoryId == options.CategoryId.Value);
        }

        if (options.MaxCookTimeMinutes.HasValue)
        {
            query = query.Where(recipe => recipe.CookTimeMinutes <= options.MaxCookTimeMinutes.Value);
        }

        if (options.MaxTotalTimeMinutes.HasValue)
        {
            query = query.Where(recipe =>
                recipe.PrepTimeMinutes + recipe.CookTimeMinutes <=
                options.MaxTotalTimeMinutes.Value);
        }

        return GetPublishedPageAsync(
            query,
            page,
            pageSize,
            options,
            cancellationToken);
    }

    public async Task<PagedResultDto<RecipeListItemDto>> SearchFullTextAsync(
        RecipeSearchQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = query.PageSize <= 0
            ? DefaultPageSize
            : Math.Min(query.PageSize, MaximumPageSize);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);

        // Chuẩn hóa từ khóa: xóa dấu tiếng Việt, lowercase — khớp với tsvector unaccent('simple') trong DB
        var keyword = VietnameseTextNormalizer.Normalize(query.Search ?? string.Empty);
        if (keyword.Length < 2)
        {
            return new PagedResultDto<RecipeListItemDto>(
                Array.Empty<RecipeListItemDto>(), 0, page, pageSize, 0);
        }

        var baseQuery = _db.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published);

        if (query.CategoryId.HasValue)
        {
            baseQuery = baseQuery.Where(recipe => recipe.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            var categorySlug = query.CategorySlug.Trim();
            baseQuery = baseQuery.Where(recipe => recipe.Category.Slug == categorySlug);
        }

        if (query.Difficulty.HasValue)
        {
            baseQuery = baseQuery.Where(recipe => recipe.Difficulty == query.Difficulty.Value);
        }

        if (query.MaxCookTimeMinutes.HasValue)
        {
            baseQuery = baseQuery.Where(recipe => recipe.CookTimeMinutes <= query.MaxCookTimeMinutes.Value);
        }

        if (query.MaxTotalTimeMinutes.HasValue)
        {
            baseQuery = baseQuery.Where(recipe =>
                recipe.PrepTimeMinutes + recipe.CookTimeMinutes <= query.MaxTotalTimeMinutes.Value);
        }

        var isInMemory = _db.Database.ProviderName?.Contains("InMemory") == true;

        // GIAI ĐOẠN 1: PostgreSQL Full-Text Search trên cột SearchVector (tsvector, unaccent, config 'simple')
        IQueryable<Recipe> matchedQuery = isInMemory
            ? baseQuery.Where(recipe =>
                recipe.Title.Contains(keyword) || recipe.Description.Contains(keyword))
            : baseQuery.Where(recipe =>
                EF.Property<NpgsqlTsVector>(recipe, "SearchVectorFts")
                    .Matches(EF.Functions.PlainToTsQuery("simple", keyword)));

        var totalCount = await matchedQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResultDto<RecipeListItemDto>(
                Array.Empty<RecipeListItemDto>(), 0, page, pageSize, 0);
        }

        // Sắp xếp theo độ liên quan ts_rank DESC, thứ cấp theo PublishedAt DESC
        IQueryable<Recipe> rankedQuery = isInMemory
            ? matchedQuery
                .OrderByDescending(recipe => recipe.PublishedAt)
                .ThenBy(recipe => recipe.Id)
            : matchedQuery
                .OrderByDescending(recipe => EF.Property<NpgsqlTsVector>(recipe, "SearchVectorFts")
                    .Rank(EF.Functions.PlainToTsQuery("simple", keyword)))
                .ThenByDescending(recipe => recipe.PublishedAt)
                .ThenBy(recipe => recipe.Id);

        var items = await rankedQuery
            .Select(ListProjection)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResultDto<RecipeListItemDto>(items, totalCount, page, pageSize, totalPages);
    }

    public Task<RecipeDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        return _db.Recipes
            .AsNoTracking()
            .AsSplitQuery()
            .Where(recipe =>
                recipe.Slug == slug &&
                recipe.Status == RecipeStatus.Published)
            .Select(recipe => new RecipeDetailDto(
                recipe.Id,
                recipe.Title,
                recipe.Slug,
                recipe.Description,
                recipe.Content,
                recipe.PrepTimeMinutes,
                recipe.CookTimeMinutes,
                recipe.Servings,
                recipe.Difficulty.ToString(),
                recipe.PublishedAt,
                new RecipeCategoryDto(
                    recipe.Category.Id,
                    recipe.Category.Name,
                    recipe.Category.Slug,
                    recipe.Category.Description),
                recipe.Ingredients
                    .Where(ingredient => !ingredient.IsDeleted)
                    .OrderBy(ingredient => ingredient.SortOrder)
                    .ThenBy(ingredient => ingredient.Id)
                    .Select(ingredient => new RecipeIngredientDto(
                        ingredient.Id,
                        ingredient.Name,
                        ingredient.Quantity,
                        ingredient.Unit,
                        ingredient.Notes,
                        ingredient.SortOrder))
                    .ToList(),
                recipe.Steps
                    .Where(step => !step.IsDeleted)
                    .OrderBy(step => step.StepNumber)
                    .ThenBy(step => step.Id)
                    .Select(step => new RecipeStepDto(
                        step.Id,
                        step.StepNumber,
                        step.Title ?? string.Empty,
                        step.Description))
                    .ToList(),
                recipe.Images
                    .Where(image => !image.IsDeleted)
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.OrderIndex)
                    .ThenBy(image => image.Id)
                    .Select(image => new RecipeImageDto(
                        image.Id,
                        image.OriginalUrl,
                        image.AltText,
                        image.IsPrimary,
                        image.OrderIndex))
                    .ToList(),
                new RecipeNutritionDto(
                    recipe.Nutrition.Calories,
                    recipe.Nutrition.Protein,
                    recipe.Nutrition.Carbohydrates,
                    recipe.Nutrition.Fat,
                    recipe.Nutrition.Fiber,
                    recipe.Nutrition.Sodium),
                recipe.Author != null
                    ? new RecipeAuthorDto(
                        recipe.Author.Id,
                        recipe.Author.DisplayName,
                        recipe.Author.AvatarUrl)
                    : null,
                recipe.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CategoryRecipesResponseDto?> GetPublishedByCategorySlugAsync(
        string categorySlug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Where(item => item.Slug == categorySlug)
            .Select(item => new RecipeCategoryDto(
                item.Id,
                item.Name,
                item.Slug,
                item.Description))
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            return null;
        }

        var recipes = await GetPublishedPageAsync(
            _db.Recipes
                .AsNoTracking()
                .Where(recipe =>
                    recipe.CategoryId == category.Id &&
                    recipe.Status == RecipeStatus.Published),
            page,
            pageSize,
            new RecipeListOptions(),
            cancellationToken);

        return new CategoryRecipesResponseDto(category, recipes);
    }

    private static async Task<PagedResultDto<RecipeListItemDto>> GetPublishedPageAsync(
        IQueryable<Recipe> query,
        int page,
        int pageSize,
        RecipeListOptions options,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaximumPageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var items = await ApplySort(query, options)
            .Select(ListProjection)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResultDto<RecipeListItemDto>(
            items,
            totalCount,
            page,
            pageSize,
            totalPages);
    }

    private static IOrderedQueryable<Recipe> ApplySort(
        IQueryable<Recipe> query,
        RecipeListOptions options)
    {
        if (options.SortBy == RecipeSortField.PublishedAt)
        {
            var publishedOrder = options.SortDescending
                ? query.OrderByDescending(recipe => recipe.PublishedAt)
                    .ThenByDescending(recipe => recipe.CreatedAt)
                : query.OrderBy(recipe => recipe.PublishedAt)
                    .ThenBy(recipe => recipe.CreatedAt);

            return publishedOrder.ThenBy(recipe => recipe.Id);
        }

        IOrderedQueryable<Recipe> ordered = options.SortBy switch
        {
            RecipeSortField.Title => options.SortDescending
                ? query.OrderByDescending(recipe => recipe.Title)
                : query.OrderBy(recipe => recipe.Title),
            RecipeSortField.PrepTime => options.SortDescending
                ? query.OrderByDescending(recipe => recipe.PrepTimeMinutes)
                : query.OrderBy(recipe => recipe.PrepTimeMinutes),
            RecipeSortField.CookTime => options.SortDescending
                ? query.OrderByDescending(recipe => recipe.CookTimeMinutes)
                : query.OrderBy(recipe => recipe.CookTimeMinutes),
            RecipeSortField.TotalTime => options.SortDescending
                ? query.OrderByDescending(recipe =>
                    recipe.PrepTimeMinutes + recipe.CookTimeMinutes)
                : query.OrderBy(recipe =>
                    recipe.PrepTimeMinutes + recipe.CookTimeMinutes),
            _ => query.OrderByDescending(recipe => recipe.PublishedAt)
                .ThenByDescending(recipe => recipe.CreatedAt)
        };

        return ordered.ThenBy(recipe => recipe.Id);
    }
}