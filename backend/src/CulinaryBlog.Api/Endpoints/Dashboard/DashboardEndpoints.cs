using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Dashboard;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard/statistics", GetStatistics).RequireAuthorization(policy => policy.RequireRole("Admin")).WithTags("Dashboard");
        app.MapGet("/api/v1/dashboard/statistics", GetStatistics).RequireAuthorization(policy => policy.RequireRole("Admin")).WithTags("Dashboard");
    }

    private static async Task<IResult> GetStatistics(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var recipes = db.Recipes.AsNoTracking().Where(recipe => !recipe.IsDeleted);
        var categoryRows = await recipes
            .GroupBy(recipe => new { recipe.CategoryId, recipe.Category.Name })
            .Select(group => new { group.Key.CategoryId, CategoryName = group.Key.Name, RecipeCount = group.Count() })
            .OrderByDescending(item => item.RecipeCount)
            .ToListAsync(cancellationToken);
        var byCategory = categoryRows
            .Select(item => new CategoryStatistic(item.CategoryId, item.CategoryName, item.RecipeCount))
            .ToList();

        var publishedCount = await recipes.CountAsync(recipe => recipe.Status == RecipeStatus.Published, cancellationToken);
        var draftCount = await recipes.CountAsync(recipe => recipe.Status == RecipeStatus.Draft, cancellationToken);
        var archivedCount = await recipes.CountAsync(recipe => recipe.Status == RecipeStatus.Archived, cancellationToken);
        var categoryCount = await db.Categories.CountAsync(category => !category.IsDeleted, cancellationToken);

        return Results.Ok(new DashboardResponse(
            categoryCount,
            await recipes.CountAsync(cancellationToken),
            publishedCount,
            draftCount,
            archivedCount,
            byCategory));
    }
}

public sealed record CategoryStatistic(Guid CategoryId, string CategoryName, int RecipeCount);
public sealed record DashboardResponse(int CategoryCount, int RecipeCount, int PublishedRecipeCount, int DraftRecipeCount, int ArchivedRecipeCount, IReadOnlyCollection<CategoryStatistic> ByCategory);
