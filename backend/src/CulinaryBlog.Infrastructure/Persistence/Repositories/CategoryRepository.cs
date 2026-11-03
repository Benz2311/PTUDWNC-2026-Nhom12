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

    public async Task<IReadOnlyList<CategoryDto>> GetAllWithRecipeCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(category => category.OrderIndex)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryDto(
                category.Id,
                category.Name,
                category.Slug,
                category.Description,
                category.ImageUrl,
                category.OrderIndex,
                category.Recipes.Count(recipe => recipe.Status == RecipeStatus.Published)))
            .ToListAsync(cancellationToken);
    }

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        _db.Categories.AsNoTracking().FirstOrDefaultAsync(category => category.Slug == slug, cancellationToken);

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Categories.AsNoTracking().FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.Categories
            .IgnoreQueryFilters()
            .AnyAsync(category => category.Name == name && (!excludeId.HasValue || category.Id != excludeId.Value), cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        _db.Categories
            .IgnoreQueryFilters()
            .AnyAsync(category => category.Slug == slug, cancellationToken);

    public Task<int> CountRecipesAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        _db.Recipes.CountAsync(recipe => recipe.CategoryId == categoryId, cancellationToken);

    public async Task<CategoryStatisticsDto> GetCategoryStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalCategories = await _db.Categories.CountAsync(cancellationToken);
        var recipeCountsByStatus = await _db.Recipes
            .AsNoTracking()
            .GroupBy(recipe => recipe.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var totalRecipes = recipeCountsByStatus.Values.Sum();
        var publishedRecipes = recipeCountsByStatus.GetValueOrDefault(RecipeStatus.Published);
        var draftRecipes = recipeCountsByStatus.GetValueOrDefault(RecipeStatus.Draft);
        var archivedRecipes = recipeCountsByStatus.GetValueOrDefault(RecipeStatus.Archived);

        var categoryCounts = await _db.Categories
            .AsNoTracking()
            .Select(category => new
            {
                category.Id,
                category.Name,
                RecipeCount = category.Recipes.Count,
            })
            .OrderByDescending(item => item.RecipeCount)
            .ThenBy(item => item.Name)
            .ToArrayAsync(cancellationToken);
        var categories = categoryCounts
            .Select(item => new CategoryStatisticItemDto(
                item.Id,
                item.Name,
                item.RecipeCount,
                CalculatePercentage(item.RecipeCount, totalRecipes)))
            .ToArray();

        var recipeDates = await _db.Recipes
            .AsNoTracking()
            .Select(recipe => recipe.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var recipesByMonth = recipeDates
            .GroupBy(date => new { date.Year, date.Month })
            .Select(group => new RecipeMonthlyStatisticDto(group.Key.Year, group.Key.Month, group.Count()))
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToArray();

        return new CategoryStatisticsDto(
            totalCategories,
            totalRecipes,
            publishedRecipes,
            draftRecipes,
            archivedRecipes,
            categories,
            categories.Take(5).ToArray(),
            recipesByMonth);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        _db.Categories.AddAsync(category, cancellationToken).AsTask();

    public void Update(Category category) => _db.Categories.Update(category);

    public void Remove(Category category) => _db.Categories.Remove(category);

    private static decimal CalculatePercentage(int count, int total) =>
        total == 0 ? 0 : Math.Round(count * 100m / total, 2);
}
