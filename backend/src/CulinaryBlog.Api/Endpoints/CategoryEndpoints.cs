using System.Security.Claims;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryStatistics;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class CategoryEndpoints
{
    private const string CategoryListCacheKey = "categories:all";

    public static void MapCategoryEndpoints(
        this WebApplication app)
    {
        var group =
            app.MapGroup("/api/v1/categories")
                .WithTags("Categories");

        // GET /api/v1/categories & /api/categories
        group.MapGet(
            "/",
            GetCategories)
            .WithName("GetCategories")
            .WithSummary("Lấy danh sách tất cả các danh mục");

        app.MapGet(
            "/api/categories",
            GetCategories)
            .ExcludeFromDescription();

        // GET /api/v1/categories/statistics
        group.MapGet(
            "/statistics",
            async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result =
                    await sender.Send(
                        new GetCategoryStatisticsQuery(),
                        cancellationToken);

                return Results.Ok(result);
            })
            .WithName("GetCategoryStatistics")
            .WithSummary("Lấy thống kê danh mục và công thức món ăn");

        // GET /api/v1/categories/{slug} (SRS FR-CAT-002 & Table 8.2)
        group.MapGet(
            "/{slug}",
            GetRecipesByCategory)
            .WithName("GetCategoryBySlug")
            .WithSummary("Lấy chi tiết danh mục và danh sách công thức thuộc danh mục");

        app.MapGet(
            "/api/categories/{slug}",
            GetRecipesByCategory)
            .ExcludeFromDescription();

        // GET /api/v1/categories/{slug}/recipes (Backward compatibility)
        group.MapGet(
            "/{slug}/recipes",
            GetRecipesByCategory)
            .WithName("GetCategoryRecipes")
            .WithSummary("Lấy danh sách công thức thuộc danh mục theo slug");
    }

    private static async Task<IResult> GetCategories(
        ISender sender,
        ICacheService cache,
        CancellationToken cancellationToken)
    {
        var cachedCategories = await cache.GetAsync<IReadOnlyList<CategoryDto>>(
            CategoryListCacheKey,
            cancellationToken);
        if (cachedCategories is not null)
        {
            return Results.Ok(cachedCategories);
        }

        var categories = await sender.Send(
            new GetCategoriesQuery(),
            cancellationToken);
        await cache.SetAsync(
            CategoryListCacheKey,
            categories,
            TimeSpan.FromMinutes(30),
            cancellationToken);

        return Results.Ok(categories);
    }

    private static async Task<IResult> GetRecipesByCategory(
        string slug,
        int? page,
        int? pageSize,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Guid? authorId = null;
        if (user.IsInRole("Author"))
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                authorId = parsedUserId;
            }
        }

        var result = await sender.Send(
            new GetRecipesByCategoryQuery(
                slug,
                page ?? 1,
                pageSize ?? 12,
                authorId),
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }
}
