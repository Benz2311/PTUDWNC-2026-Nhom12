using System.Linq.Expressions;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
                .ThenBy(image => image.SortOrder)
                .ThenBy(image => image.Id)
                .Select(image => image.Url)
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
        CancellationToken cancellationToken = default)
    {
        return GetPublishedPageAsync(
            _db.Recipes
                .AsNoTracking()
                .Where(recipe => recipe.Status == RecipeStatus.Published),
            page,
            pageSize,
            cancellationToken);
    }

    public Task<RecipeDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        return _db.Recipes
            .AsNoTracking()
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
                        step.Title,
                        step.Description))
                    .ToList(),
                recipe.Images
                    .Where(image => !image.IsDeleted)
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.SortOrder)
                    .ThenBy(image => image.Id)
                    .Select(image => new RecipeImageDto(
                        image.Id,
                        image.Url,
                        image.AltText,
                        image.IsPrimary,
                        image.SortOrder))
                    .ToList(),
                new RecipeNutritionDto(
                    recipe.Nutrition.Calories,
                    recipe.Nutrition.Protein,
                    recipe.Nutrition.Carbohydrates,
                    recipe.Nutrition.Fat,
                    recipe.Nutrition.Fiber,
                    recipe.Nutrition.Sodium)))
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
            cancellationToken);

        return new CategoryRecipesResponseDto(category, recipes);
    }

    private static async Task<PagedResultDto<RecipeListItemDto>> GetPublishedPageAsync(
        IQueryable<Recipe> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaximumPageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var items = await query
            .OrderByDescending(recipe => recipe.PublishedAt)
            .ThenByDescending(recipe => recipe.CreatedAt)
            .ThenBy(recipe => recipe.Id)
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
}