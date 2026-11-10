using System.Security.Claims;
using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Recipes;

public static class RecipeEngagementEndpoints
{
    public static void MapRecipeEngagementEndpoints(this IEndpointRouteBuilder app)
    {
        var recipesGroup = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipe Engagement & Discovery");

        recipesGroup.MapPost("/{slug}/view", RecordRecipeView)
            .WithName("RecordRecipeView")
            .WithSummary("Ghi nhận lượt xem hợp lệ cho công thức với cơ chế chống F5 spam");

        recipesGroup.MapGet("/trending", GetTrendingRecipes)
            .WithName("GetTrendingRecipes")
            .WithSummary("Khám phá các công thức thịnh hành dựa trên lượt xem hợp lệ trong 7 ngày hoặc 30 ngày");

        recipesGroup.MapGet("/recommendations", GetRecommendations)
            .WithName("GetRecommendations")
            .WithSummary("Gợi ý công thức cá nhân hóa theo danh mục quan tâm hoặc công thức phổ biến");
    }

    private static Guid? GetUserId(ClaimsPrincipal? user)
    {
        if (user == null) return null;
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(idStr, out var id) ? id : null;
    }

    private static async Task<IResult> RecordRecipeView(
        string slug,
        HttpContext httpContext,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var recipe = await db.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Slug == slug && !r.IsDeleted, ct);

        if (recipe == null)
        {
            return Results.Problem(
                title: "Recipe not found",
                detail: $"Công thức '{slug}' không tồn tại.",
                statusCode: StatusCodes.Status404NotFound,
                type: "RECIPE_NOT_FOUND");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        if (userAgent.Length > 250) userAgent = userAgent[..250];

        var userId = GetUserId(httpContext.User);

        // Anti-spam rule: Chặn tính lượt xem trùng lặp trong vòng 30 phút từ cùng IP hoặc cùng User
        var windowStart = DateTime.UtcNow.AddMinutes(-30);

        var hasRecentView = await db.RecipeViews
            .AsNoTracking()
            .AnyAsync(v => v.RecipeId == recipe.Id
                && v.ViewedAt >= windowStart
                && ((userId != null && v.UserId == userId) || v.IpAddress == ip), ct);

        if (hasRecentView)
        {
            return Results.Ok(ApiResponse.Success(new { recorded = false, reason = "View already recorded recently within cooldown window." }));
        }

        var view = new RecipeView
        {
            RecipeId = recipe.Id,
            UserId = userId,
            IpAddress = ip,
            UserAgent = userAgent,
            ViewedAt = DateTime.UtcNow
        };

        db.RecipeViews.Add(view);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse.Success(new { recorded = true }));
    }

    private static async Task<IResult> GetTrendingRecipes(
        string? period,
        Guid? categoryId,
        string? categorySlug,
        int? limit,
        ICacheService cache,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var days = period == "30d" ? 30 : 7;
        var take = Math.Clamp(limit ?? 10, 1, 50);

        var cacheKey = $"trending:recipes:{days}d:{categoryId}:{categorySlug}:{take}";
        var cached = await cache.GetAsync<IReadOnlyList<TrendingRecipeDto>>(cacheKey, ct);
        if (cached != null)
        {
            return Results.Ok(ApiResponse.Success(cached));
        }

        var since = DateTime.UtcNow.AddDays(-days);

        // Subquery counting views in the requested period
        var viewsQuery = db.RecipeViews
            .Where(v => v.ViewedAt >= since);

        var recipesQuery = db.Recipes
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Author)
            .Include(r => r.Images)
            .Where(r => r.Status == RecipeStatus.Published && !r.IsDeleted);

        if (categoryId.HasValue)
        {
            recipesQuery = recipesQuery.Where(r => r.CategoryId == categoryId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            recipesQuery = recipesQuery.Where(r => r.Category.Slug == categorySlug.Trim());
        }

        var trendingList = await recipesQuery
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Slug,
                CoverImageUrl = r.CoverImageUrl ?? r.Images.OrderBy(img => img.SortOrder).Select(img => img.OriginalUrl).FirstOrDefault(),
                CategoryName = r.Category.Name,
                CategorySlug = r.Category.Slug,
                AuthorName = r.Author.DisplayName ?? r.Author.UserName,
                ViewsInPeriod = db.RecipeViews.Count(v => v.RecipeId == r.Id && v.ViewedAt >= since),
                r.PrepTimeMinutes,
                r.CookTimeMinutes,
                Difficulty = r.Difficulty.ToString(),
                r.PublishedAt
            })
            .OrderByDescending(r => r.ViewsInPeriod)
            .ThenByDescending(r => r.PublishedAt)
            .Take(take)
            .ToListAsync(ct);

        var rank = 1;
        var dtos = trendingList.Select(r => new TrendingRecipeDto(
            rank++,
            r.Id,
            r.Title,
            r.Slug,
            r.CoverImageUrl,
            r.CategoryName,
            r.CategorySlug,
            r.AuthorName,
            r.ViewsInPeriod,
            r.PrepTimeMinutes,
            r.CookTimeMinutes,
            r.Difficulty,
            r.PublishedAt
        )).ToList();

        // Cache 5 minutes
        await cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5), ct);

        return Results.Ok(ApiResponse.Success(dtos));
    }

    private static async Task<IResult> GetRecommendations(
        int? limit,
        ClaimsPrincipal? userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var take = Math.Clamp(limit ?? 6, 1, 20);
        var userId = GetUserId(userPrincipal);

        List<Guid> preferredCategoryIds = new();

        if (userId.HasValue)
        {
            // 1. Phân tích các category từ lịch sử xem trong 30 ngày qua
            var viewedCategoryIds = await db.RecipeViews
                .AsNoTracking()
                .Where(v => v.UserId == userId.Value && v.ViewedAt >= DateTime.UtcNow.AddDays(-30))
                .GroupBy(v => v.Recipe.CategoryId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(4)
                .ToListAsync(ct);

            // 2. Phân tích các category từ bộ sưu tập cá nhân
            var collectionCategoryIds = await db.RecipeCollectionItems
                .AsNoTracking()
                .Where(i => i.Collection.UserId == userId.Value && !i.Collection.IsDeleted)
                .GroupBy(i => i.Recipe.CategoryId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(4)
                .ToListAsync(ct);

            preferredCategoryIds = viewedCategoryIds
                .Concat(collectionCategoryIds)
                .Distinct()
                .ToList();
        }

        var baseQuery = db.Recipes
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Author)
            .Include(r => r.Images)
            .Where(r => r.Status == RecipeStatus.Published && !r.IsDeleted);

        List<Recipe> recommendedRecipes = new();

        if (preferredCategoryIds.Count > 0)
        {
            // Ưu tiên các món ăn thuộc danh mục yêu thích
            recommendedRecipes = await baseQuery
                .Where(r => preferredCategoryIds.Contains(r.CategoryId))
                .OrderByDescending(r => r.PublishedAt)
                .Take(take)
                .ToListAsync(ct);
        }

        // Nếu người dùng chưa có lịch sử hoặc số lượng gợi ý chưa đủ, bổ sung các món phổ biến / mới nhất
        if (recommendedRecipes.Count < take)
        {
            var existingIds = recommendedRecipes.Select(r => r.Id).ToList();
            var needed = take - recommendedRecipes.Count;

            var fallbackRecipes = await baseQuery
                .Where(r => !existingIds.Contains(r.Id))
                .OrderByDescending(r => r.PublishedAt)
                .Take(needed)
                .ToListAsync(ct);

            recommendedRecipes.AddRange(fallbackRecipes);
        }

        var result = recommendedRecipes.Select(r => new RecipeListItemDto(
            r.Id,
            r.Title,
            r.Slug,
            r.Description,
            r.PrepTimeMinutes,
            r.CookTimeMinutes,
            r.Servings,
            r.Difficulty.ToString(),
            r.CoverImageUrl ?? r.Images.OrderBy(img => img.SortOrder).Select(img => img.OriginalUrl).FirstOrDefault(),
            new RecipeCategoryDto(r.Category.Id, r.Category.Name, r.Category.Slug, r.Category.Description),
            r.PublishedAt
        )).ToList();

        return Results.Ok(ApiResponse.Success(result));
    }
}

