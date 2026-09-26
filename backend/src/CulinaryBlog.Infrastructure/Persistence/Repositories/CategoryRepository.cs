using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
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
            .OrderBy(category => category.OrderIndex)
            .ThenBy(category => category.Name)
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

        var totalRecipes =
            await _db.Recipes.CountAsync(
                cancellationToken);

        var publishedRecipes =
            await _db.Recipes.CountAsync(
                r => r.Status == RecipeStatus.Published,
                cancellationToken);

        var draftRecipes =
            await _db.Recipes.CountAsync(
                r => r.Status == RecipeStatus.Draft,
                cancellationToken);

        var categories =
            await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c =>
                    new CulinaryBlog.Application.DTOs.CategoryStatisticItemDto(
                        c.Id,
                        c.Name,
                        c.Recipes.Count(
                            r => r.Status ==
                                 RecipeStatus.Published)))
                .ToArrayAsync(cancellationToken);

        return new CulinaryBlog.Application.DTOs.CategoryStatisticsDto(
            totalCategories,
            totalRecipes,
            publishedRecipes,
            draftRecipes,
            categories);
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
}