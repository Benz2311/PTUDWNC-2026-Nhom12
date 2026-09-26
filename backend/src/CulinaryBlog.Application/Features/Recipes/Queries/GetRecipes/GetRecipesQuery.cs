using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;

public record GetRecipesQuery(int Page = 1, int PageSize = 12)
    : IRequest<PagedResultDto<RecipeListItemDto>>;