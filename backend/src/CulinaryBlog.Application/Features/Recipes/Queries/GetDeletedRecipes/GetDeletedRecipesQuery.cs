using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetDeletedRecipes;

public sealed record GetDeletedRecipesQuery(
    int Page = 1,
    int PageSize = 12) : IRequest<PagedResultDto<RecipeTrashItemDto>>;

public sealed class GetDeletedRecipesQueryHandler
    : IRequestHandler<GetDeletedRecipesQuery, PagedResultDto<RecipeTrashItemDto>>
{
    private readonly IRecipeRepository _recipeRepository;

    public GetDeletedRecipesQueryHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public Task<PagedResultDto<RecipeTrashItemDto>> Handle(
        GetDeletedRecipesQuery request,
        CancellationToken cancellationToken)
    {
        return _recipeRepository.GetDeletedAsync(
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}