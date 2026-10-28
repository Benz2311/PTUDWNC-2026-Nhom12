using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;

public class GetRecipesByCategoryQueryHandler
    : IRequestHandler<GetRecipesByCategoryQuery, CategoryRecipesResponseDto?>
{
    private readonly IRecipeRepository _recipeRepository;

    public GetRecipesByCategoryQueryHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public Task<CategoryRecipesResponseDto?> Handle(
        GetRecipesByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        return _recipeRepository.GetPublishedByCategorySlugAsync(
            request.CategorySlug,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}