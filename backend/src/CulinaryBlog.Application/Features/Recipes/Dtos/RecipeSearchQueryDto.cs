using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Features.Recipes.Dtos;

public record RecipeSearchQueryDto(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? CategoryId = null,
    string? CategorySlug = null,
    DifficultyLevel? Difficulty = null,
    int? MaxTotalTimeMinutes = null,
    int? MaxCookTimeMinutes = null,
    string SortBy = "PublishedAt",
    bool SortDescending = true);