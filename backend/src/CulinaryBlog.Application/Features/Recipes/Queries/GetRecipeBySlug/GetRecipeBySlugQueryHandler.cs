using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;

public class GetRecipeBySlugQueryHandler
    : IRequestHandler<GetRecipeBySlugQuery, RecipeDetailDto?>
{
    private readonly IRecipeRepository _recipeRepository;

    public GetRecipeBySlugQueryHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public Task<RecipeDetailDto?> Handle(
        GetRecipeBySlugQuery request,
        CancellationToken cancellationToken)
    {
        return _recipeRepository.GetPublishedBySlugAsync(
            request.Slug,
            cancellationToken);
    }
}