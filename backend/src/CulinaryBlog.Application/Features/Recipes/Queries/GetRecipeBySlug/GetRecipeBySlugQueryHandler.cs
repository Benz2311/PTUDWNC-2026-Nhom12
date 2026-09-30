using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;

public class GetRecipeBySlugQueryHandler
{
    private readonly IRecipeRepository _recipeRepository;

    public GetRecipeBySlugQueryHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public Task<RecipeDetailDto?> Handle(
        GetRecipeBySlugQuery request,
        CancellationToken cancellationToken = default)
    {
        return _recipeRepository.GetPublishedBySlugAsync(
            request.Slug,
            cancellationToken);
    }
}