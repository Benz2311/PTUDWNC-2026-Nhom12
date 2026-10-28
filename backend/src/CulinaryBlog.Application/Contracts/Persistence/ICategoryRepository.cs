using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Contracts.Persistence;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryDto>> GetAllWithRecipeCountAsync(
        CancellationToken cancellationToken = default);

    Task<Category?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountRecipesAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<CategoryStatisticsDto> GetCategoryStatisticsAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default);

    void Update(Category category);

    void Remove(Category category);
}
