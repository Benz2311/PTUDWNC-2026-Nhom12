using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _db;

    public CategoryRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryDto>>
        GetAllWithRecipeCountAsync(
            CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(
                category.Id,
                category.Name,
                category.Slug,
                category.Description,
                category.ImageUrl,
                category.OrderIndex,
                category.Recipes.Count(recipe =>
                    recipe.Status == RecipeStatus.Published)))
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Slug == slug,
                cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == id,
                cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AnyAsync(
                c =>
                    c.Name == name &&
                    (!excludeId.HasValue ||
                     c.Id != excludeId.Value),
                cancellationToken);
    }

    public async Task<int> CountRecipesAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Recipes
            .CountAsync(
                r => r.CategoryId == categoryId,
                cancellationToken);
    }

    public async Task<CulinaryBlog.Application.DTOs.CategoryStatisticsDto> GetCategoryStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalCategories =
            await _db.Categories.CountAsync(
                cancellationToken);

        var recipeCountsByStatus = await _db.Recipes
            .AsNoTracking()
            .GroupBy(recipe => recipe.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count(),
            })
            .ToDictionaryAsync(
                item => item.Status,
                item => item.Count,
                cancellationToken);
        var totalRecipes = recipeCountsByStatus.Values.Sum();
        var publishedRecipes = recipeCountsByStatus.GetValueOrDefault(RecipeStatus.Published);
        var draftRecipes = recipeCountsByStatus.GetValueOrDefault(RecipeStatus.Draft);
        var archivedRecipes = recipeCountsByStatus.GetValueOrDefault(RecipeStatus.Archived);

        var categoryCounts =
            await _db.Categories
                .AsNoTracking()
                .Select(c =>
                    new
                    {
                        c.Id,
                        c.Name,
                        RecipeCount = c.Recipes.Count(
                            recipe => recipe.Status == RecipeStatus.Published),
                    })
                .OrderByDescending(item => item.RecipeCount)
                .ThenBy(item => item.Name)
                .ToArrayAsync(cancellationToken);

        var categories = categoryCounts
            .Select(item => new CulinaryBlog.Application.DTOs.CategoryStatisticItemDto(
                item.Id,
                item.Name,
                item.RecipeCount,
                CalculatePercentage(item.RecipeCount, publishedRecipes)))
            .ToArray();

        var recipesByMonth = await _db.Recipes
            .AsNoTracking()
            .GroupBy(recipe => new
            {
                recipe.CreatedAt.Year,
                recipe.CreatedAt.Month,
            })
            .Select(group => new CulinaryBlog.Application.DTOs.RecipeMonthlyStatisticDto(
                group.Key.Year,
                group.Key.Month,
                group.Count()))
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToArrayAsync(cancellationToken);

        return new CulinaryBlog.Application.DTOs.CategoryStatisticsDto(
            totalCategories,
            totalRecipes,
            publishedRecipes,
            draftRecipes,
            archivedRecipes,
            categories,
            categories.Take(5).ToArray(),
            recipesByMonth);
    }

    public async Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        await _db.Categories.AddAsync(
            category,
            cancellationToken);
    }

    public void Update(Category category)
    {
        _db.Categories.Update(category);
    }

    public void Remove(Category category)
    {
        _db.Categories.Remove(category);
    }

    private static decimal CalculatePercentage(int count, int total)
    {
        return total == 0
            ? 0
            : Math.Round(count * 100m / total, 2);
    }
}
