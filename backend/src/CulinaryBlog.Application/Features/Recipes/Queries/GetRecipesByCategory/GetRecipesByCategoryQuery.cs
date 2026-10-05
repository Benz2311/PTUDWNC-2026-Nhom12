using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;

public record GetRecipesByCategoryQuery(
    string CategorySlug,
    int Page = 1,
    int PageSize = 12)
    : IRequest<CategoryRecipesResponseDto?>;