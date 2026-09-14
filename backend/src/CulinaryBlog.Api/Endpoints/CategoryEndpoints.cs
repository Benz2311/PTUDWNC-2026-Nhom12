using CulinaryBlog.Application.Features.Categories.Queries
    .GetCategoryStatistics;
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
            });
    }
}