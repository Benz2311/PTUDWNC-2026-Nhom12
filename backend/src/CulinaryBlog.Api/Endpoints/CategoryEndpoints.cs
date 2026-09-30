using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryStatistics;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(
        this WebApplication app)
    {
        var group =
            app.MapGroup("/api/v1/categories")
                .WithTags("Categories");

        // GET /api/v1/categories & /api/categories
        group.MapGet(
            "/",
            async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result =
                    await sender.Send(
                        new GetCategoriesQuery(),
                        cancellationToken);

                return Results.Ok(result);
            })
            .WithName("GetCategories")
            .WithSummary("Lấy danh sách tất cả các danh mục");

        app.MapGet(
            "/api/categories",
            async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result =
                    await sender.Send(
                        new GetCategoriesQuery(),
                        cancellationToken);

                return Results.Ok(result);
            })
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
            async (
                string slug,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result =
                    await sender.Send(
                        new GetRecipesByCategoryQuery(slug, page ?? 1, pageSize ?? 12),
                        cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            })
            .WithName("GetCategoryBySlug")
            .WithSummary("Lấy chi tiết danh mục và danh sách công thức thuộc danh mục");

        app.MapGet(
            "/api/categories/{slug}",
            async (
                string slug,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result =
                    await sender.Send(
                        new GetRecipesByCategoryQuery(slug, page ?? 1, pageSize ?? 12),
                        cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            })
            .ExcludeFromDescription();

        // GET /api/v1/categories/{slug}/recipes (Backward compatibility)
        group.MapGet(
            "/{slug}/recipes",
            async (
                string slug,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result =
                    await sender.Send(
                        new GetRecipesByCategoryQuery(slug, page ?? 1, pageSize ?? 12),
                        cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            })
            .WithName("GetCategoryRecipes")
            .WithSummary("Lấy danh sách công thức thuộc danh mục theo slug");
    }
}