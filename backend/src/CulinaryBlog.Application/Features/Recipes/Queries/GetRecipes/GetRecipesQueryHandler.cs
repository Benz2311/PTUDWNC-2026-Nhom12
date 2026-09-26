using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;

public class GetRecipesQueryHandler
    : IRequestHandler<GetRecipesQuery, PagedResultDto<RecipeListItemDto>>
{
    private readonly IRecipeRepository _recipeRepository;

    public GetRecipesQueryHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public Task<PagedResultDto<RecipeListItemDto>> Handle(
        GetRecipesQuery request,
        CancellationToken cancellationToken)
    {
        return _recipeRepository.GetPublishedAsync(
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}