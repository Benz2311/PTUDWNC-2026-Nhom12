using CulinaryBlog.Application.DTOs;

namespace CulinaryBlog.Application.Contracts.Persistence;

public interface IRecipeRepository
{
    Task<PagedResultDto<RecipeListItemDto>> GetPublishedAsync(
        int page,
        int pageSize,
        RecipeListOptions options,
        CancellationToken cancellationToken = default);

    Task<RecipeDetailDto?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<CategoryRecipesResponseDto?> GetPublishedByCategorySlugAsync(
        string categorySlug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}