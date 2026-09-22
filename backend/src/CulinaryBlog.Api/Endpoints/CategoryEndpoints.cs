using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryStatistics;
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

        // GET /api/v1/categories
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
    }
}