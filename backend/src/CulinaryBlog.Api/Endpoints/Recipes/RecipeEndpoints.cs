using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipes");

        group.MapGet("/", GetRecipes)
            .WithName("GetRecipes")
            .WithSummary("Lấy danh sách công thức đã xuất bản");

        group.MapGet("/{slug}", GetRecipeBySlug)
            .WithName("GetRecipeBySlug")
            .WithSummary("Lấy chi tiết công thức đã xuất bản");
    }

    private static async Task<IResult> GetRecipes(
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetRecipesQuery(page ?? 1, pageSize ?? 12),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecipeBySlug(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetRecipeBySlugQuery(slug),
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }
}