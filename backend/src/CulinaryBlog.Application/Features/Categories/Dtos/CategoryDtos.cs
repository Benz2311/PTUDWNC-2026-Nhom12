using CulinaryBlog.Application.DTOs;

namespace CulinaryBlog.Application.Features.Categories.Dtos;

public record CreateCategoryDto(
    string Name,
    string? Slug = null,
    string? Description = null,
    string? ImageUrl = null,
    int OrderIndex = 0);

public record UpdateCategoryDto(
    string Name,
    string? Description = null,
    string? ImageUrl = null,
    int? OrderIndex = null,
    string? Slug = null);