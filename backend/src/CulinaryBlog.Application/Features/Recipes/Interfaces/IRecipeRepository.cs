using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Features.Recipes.Interfaces;

/// <summary>
/// Giao diện Repository chuyên biệt cho Recipe phục vụ truy vấn và quản lý hình ảnh/bước nấu (Lab 3)
/// </summary>
public interface IRecipeRepository : IRepository<Recipe>
{
    Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Recipe?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecipeImage>> GetActiveImagesAsync(Guid recipeId, CancellationToken cancellationToken = default);
}
