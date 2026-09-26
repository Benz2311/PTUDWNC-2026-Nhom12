using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.DTOs;

public enum RecipeSortField
{
    PublishedAt,
    Title,
    PrepTime,
    CookTime,
    TotalTime
}

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record RecipeCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description);

public record RecipeListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string Difficulty,
    string? ImageUrl,
    RecipeCategoryDto Category,
    DateTime? PublishedAt);

public record RecipeIngredientDto(
    Guid Id,
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    int SortOrder);

public record RecipeStepDto(
    Guid Id,
    int StepNumber,
    string Title,
    string Description);

public record RecipeImageDto(
    Guid Id,
    string Url,
    string? AltText,
    bool IsPrimary,
    int SortOrder);

public record RecipeNutritionDto(
    decimal? Calories,
    decimal? Protein,
    decimal? Carbohydrates,
    decimal? Fat,
    decimal? Fiber,
    decimal? Sodium);

public record RecipeDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string Content,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string Difficulty,
    DateTime? PublishedAt,
    RecipeCategoryDto Category,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps,
    IReadOnlyList<RecipeImageDto> Images,
    RecipeNutritionDto Nutrition);

public record CategoryRecipesResponseDto(
    RecipeCategoryDto Category,
    PagedResultDto<RecipeListItemDto> Recipes);

public record RecipeListOptions(
    string? Search = null,
    string? CategorySlug = null,
    DifficultyLevel? Difficulty = null,
    int? MaxTotalTimeMinutes = null,
    RecipeSortField SortBy = RecipeSortField.PublishedAt,
    bool SortDescending = true);