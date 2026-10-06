using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Dtos;

namespace CulinaryBlog.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}