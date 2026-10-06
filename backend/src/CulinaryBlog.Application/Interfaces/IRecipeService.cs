using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Recipes.Dtos;

namespace CulinaryBlog.Application.Interfaces;

public interface IRecipeService
{
    Task<RecipeDetailDto> CreateAsync(CreateRecipeDto dto, Guid authorId, CancellationToken cancellationToken = default);

    Task<RecipeDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RecipeDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<PagedResultDto<RecipeListItemDto>> GetPagedListAsync(RecipeSearchQueryDto query, CancellationToken cancellationToken = default);

    Task<PagedResultDto<RecipeListItemDto>> SearchRecipesAsync(RecipeSearchQueryDto query, CancellationToken cancellationToken = default);

    Task<RecipeDetailDto> UpdateAsync(Guid id, UpdateRecipeDto dto, Guid authorId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, Guid authorId, CancellationToken cancellationToken = default);
}