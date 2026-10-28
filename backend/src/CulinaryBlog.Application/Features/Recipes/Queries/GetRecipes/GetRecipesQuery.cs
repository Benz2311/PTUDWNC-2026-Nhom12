using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;

public record GetRecipesQuery(
    int Page = 1,
    int PageSize = 12,
    RecipeListOptions? Options = null)
    : IRequest<PagedResultDto<RecipeListItemDto>>;