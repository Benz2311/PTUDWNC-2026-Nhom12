using CulinaryBlog.Application.DTOs;

namespace CulinaryBlog.Application.Features.Recipes.Dtos;

public record CreateRecipeIngredientDto(
    string Name,
    decimal? Quantity = null,
    string? Unit = null,
    string? Notes = null,
    int SortOrder = 0);

public record CreateRecipeStepDto(
    int StepNumber,
    string Title,
    string Description,
    int? TimerMinutes = null,
    string? ImageUrl = null);

public record CreateRecipeNutritionDto(
    decimal? Calories = null,
    decimal? Protein = null,
    decimal? Carbohydrates = null,
    decimal? Fat = null,
    decimal? Fiber = null,
    decimal? Sodium = null);

public record CreateRecipeDto(
    string Title,
    string? Slug = null,
    string? Description = null,
    string Content = "",
    int PrepTimeMinutes = 0,
    int CookTimeMinutes = 0,
    int Servings = 1,
    string Difficulty = "Medium",
    Guid CategoryId = default,
    IReadOnlyList<CreateRecipeIngredientDto> Ingredients = null!,
    IReadOnlyList<CreateRecipeStepDto> Steps = null!,
    CreateRecipeNutritionDto? Nutrition = null);

public record UpdateRecipeDto(
    string Title,
    string? Description = null,
    string Content = "",
    int PrepTimeMinutes = 0,
    int CookTimeMinutes = 0,
    int Servings = 1,
    string Difficulty = "Medium",
    Guid CategoryId = default,
    IReadOnlyList<CreateRecipeIngredientDto> Ingredients = null!,
    IReadOnlyList<CreateRecipeStepDto> Steps = null!,
    CreateRecipeNutritionDto? Nutrition = null,
    byte[]? RowVersion = null);