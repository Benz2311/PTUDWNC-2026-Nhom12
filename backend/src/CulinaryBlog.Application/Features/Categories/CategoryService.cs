using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Exceptions;

namespace CulinaryBlog.Application.Features.Categories;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _categoryRepository.GetAllWithRecipeCountAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return null;
        }

        var recipeCount = await _categoryRepository.CountRecipesAsync(id, cancellationToken);
        return MapToDto(category, recipeCount);
    }

    public async Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetBySlugAsync(slug, cancellationToken);
        if (category == null)
        {
            return null;
        }

        var recipeCount = await _categoryRepository.CountRecipesAsync(category.Id, cancellationToken);
        return MapToDto(category, recipeCount);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (await _categoryRepository.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Category with name '{dto.Name}' already exists.");
        }

        var category = Category.Create(
            dto.Name,
            dto.Slug,
            dto.Description,
            dto.ImageUrl,
            dto.OrderIndex);

        await _categoryRepository.AddAsync(category, cancellationToken);

        return MapToDto(category, 0);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category", id);

        if (await _categoryRepository.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            throw new ConflictException($"Category with name '{dto.Name}' already exists.");
        }

        category.Update(
            dto.Name,
            dto.Description,
            dto.ImageUrl,
            dto.OrderIndex,
            dto.Slug);

        _categoryRepository.Update(category);

        var recipeCount = await _categoryRepository.CountRecipesAsync(id, cancellationToken);
        return MapToDto(category, recipeCount);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category", id);

        var recipeCount = await _categoryRepository.CountRecipesAsync(id, cancellationToken);
        if (recipeCount > 0)
        {
            throw new InvalidOperationException("Cannot delete category with associated recipes.");
        }

        category.SoftDelete();
        _categoryRepository.Update(category);
    }

    private static CategoryDto MapToDto(Category category, int recipeCount)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.OrderIndex,
            recipeCount);
    }
}