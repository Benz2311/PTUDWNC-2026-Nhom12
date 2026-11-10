using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Admin;

public static class AdminAnalyticsEndpoints
{
    public static void MapAdminAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/analytics")
            .WithTags("Admin Analytics")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/activity", GetActivityAnalytics)
            .WithName("GetActivityAnalytics")
            .WithSummary("Thống kê hoạt động đăng tải và xuất bản công thức theo thời gian thực tế");

        group.MapGet("/category-performance", GetCategoryPerformanceAnalytics)
            .WithName("GetCategoryPerformanceAnalytics")
            .WithSummary("Phân tích hiệu quả hoạt động và tốc độ tăng trưởng của từng danh mục");
    }

    private static async Task<IResult> GetActivityAnalytics(
        int? days,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var periodDays = days == 30 ? 30 : 7;
        var now = DateTime.UtcNow;
        var today = now.Date;

        var currentStart = today.AddDays(-periodDays + 1);
        var previousStart = currentStart.AddDays(-periodDays);

        // Lấy danh sách Recipe trong 2 chu kỳ để so sánh
        var recipes = await db.Recipes
            .AsNoTracking()
            .Include(r => r.Author)
            .Where(r => !r.IsDeleted && r.CreatedAt >= previousStart)
            .ToListAsync(ct);

        // 1. Daily Stats trong chu kỳ hiện tại
        var dailyStats = new List<DailyActivityStatDto>();
        for (int i = 0; i < periodDays; i++)
        {
            var date = currentStart.AddDays(i);
            var nextDate = date.AddDays(1);
            var dateStr = date.ToString("yyyy-MM-dd");

            var created = recipes.Count(r => r.CreatedAt >= date && r.CreatedAt < nextDate);
            var published = recipes.Count(r => r.PublishedAt.HasValue
                && r.PublishedAt.Value >= date
                && r.PublishedAt.Value < nextDate
                && r.Status == RecipeStatus.Published);

            dailyStats.Add(new DailyActivityStatDto(dateStr, created, published));
        }

        // 2. Summary comparison
        var totalCreatedCurrent = recipes.Count(r => r.CreatedAt >= currentStart);
        var totalCreatedPrevious = recipes.Count(r => r.CreatedAt >= previousStart && r.CreatedAt < currentStart);
        var createdGrowthRate = totalCreatedPrevious > 0
            ? Math.Round(((totalCreatedCurrent - totalCreatedPrevious) / (double)totalCreatedPrevious) * 100, 1)
            : (totalCreatedCurrent > 0 ? 100.0 : 0.0);

        var totalPublishedCurrent = recipes.Count(r => r.PublishedAt.HasValue && r.PublishedAt.Value >= currentStart && r.Status == RecipeStatus.Published);
        var totalPublishedPrevious = recipes.Count(r => r.PublishedAt.HasValue && r.PublishedAt.Value >= previousStart && r.PublishedAt.Value < currentStart && r.Status == RecipeStatus.Published);
        var publishedGrowthRate = totalPublishedPrevious > 0
            ? Math.Round(((totalPublishedCurrent - totalPublishedPrevious) / (double)totalPublishedPrevious) * 100, 1)
            : (totalPublishedCurrent > 0 ? 100.0 : 0.0);

        var summary = new ActivityComparisonDto(
            totalCreatedCurrent,
            totalCreatedPrevious,
            createdGrowthRate,
            totalPublishedCurrent,
            totalPublishedPrevious,
            publishedGrowthRate
        );

        // 3. Top Active Authors trong chu kỳ hiện tại
        var currentRecipes = recipes.Where(r => r.CreatedAt >= currentStart).ToList();
        var topAuthors = currentRecipes
            .GroupBy(r => r.AuthorId)
            .Select(g =>
            {
                var author = g.First().Author;
                var createdCount = g.Count();
                var publishedCount = g.Count(r => r.Status == RecipeStatus.Published);
                return new TopAuthorActivityDto(
                    g.Key,
                    author?.DisplayName ?? author?.UserName ?? "Tác giả",
                    author?.Email ?? "",
                    author?.AvatarUrl,
                    createdCount,
                    publishedCount
                );
            })
            .OrderByDescending(a => a.RecipesPublished)
            .ThenByDescending(a => a.RecipesCreated)
            .Take(10)
            .ToList();

        var result = new ActivityAnalyticsDto(
            periodDays,
            dailyStats,
            summary,
            topAuthors
        );

        return Results.Ok(ApiResponse.Success(result));
    }

    private static async Task<IResult> GetCategoryPerformanceAnalytics(
        int? days,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var periodDays = days == 30 ? 30 : 7;
        var now = DateTime.UtcNow;
        var today = now.Date;

        var currentStart = today.AddDays(-periodDays + 1);
        var previousStart = currentStart.AddDays(-periodDays);

        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync(ct);

        var recipes = await db.Recipes
            .AsNoTracking()
            .Where(r => !r.IsDeleted && (r.CreatedAt >= previousStart || (r.PublishedAt.HasValue && r.PublishedAt.Value >= previousStart)))
            .ToListAsync(ct);

        // Lấy thống kê view trong chu kỳ hiện tại
        var viewsPerRecipe = await db.RecipeViews
            .AsNoTracking()
            .Where(v => v.ViewedAt >= currentStart)
            .GroupBy(v => v.RecipeId)
            .Select(g => new { RecipeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RecipeId, x => x.Count, ct);

        var items = new List<CategoryPerformanceItemDto>();

        foreach (var cat in categories)
        {
            var catRecipesCurrent = recipes.Where(r => r.CategoryId == cat.Id && r.CreatedAt >= currentStart).ToList();
            var newCount = catRecipesCurrent.Count;

            var publishedCurrent = recipes.Count(r => r.CategoryId == cat.Id
                && r.PublishedAt.HasValue
                && r.PublishedAt.Value >= currentStart
                && r.Status == RecipeStatus.Published);

            var publishedPrevious = recipes.Count(r => r.CategoryId == cat.Id
                && r.PublishedAt.HasValue
                && r.PublishedAt.Value >= previousStart
                && r.PublishedAt.Value < currentStart
                && r.Status == RecipeStatus.Published);

            var growthRate = publishedPrevious > 0
                ? Math.Round(((publishedCurrent - publishedPrevious) / (double)publishedPrevious) * 100, 1)
                : (publishedCurrent > 0 ? 100.0 : 0.0);

            // Tổng views của các recipe thuộc category này trong kỳ
            var catRecipeIds = recipes.Where(r => r.CategoryId == cat.Id).Select(r => r.Id).ToHashSet();
            var totalViews = viewsPerRecipe
                .Where(kvp => catRecipeIds.Contains(kvp.Key))
                .Sum(kvp => kvp.Value);

            items.Add(new CategoryPerformanceItemDto(
                cat.Id,
                cat.Name,
                cat.Slug,
                newCount,
                publishedCurrent,
                publishedPrevious,
                growthRate,
                totalViews
            ));
        }

        var fastestGrowing = items
            .Where(i => i.GrowthRate > 0 || i.PublishedRecipesCount > 0)
            .OrderByDescending(i => i.GrowthRate)
            .ThenByDescending(i => i.PublishedRecipesCount)
            .Take(5)
            .ToList();

        var lowActivity = items
            .Where(i => i.NewRecipesCount == 0 || i.PublishedRecipesCount == 0)
            .OrderBy(i => i.PublishedRecipesCount)
            .ThenBy(i => i.NewRecipesCount)
            .Take(5)
            .ToList();

        var result = new CategoryPerformanceAnalyticsDto(
            periodDays,
            items,
            fastestGrowing,
            lowActivity
        );

        return Results.Ok(ApiResponse.Success(result));
    }
}

