using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>Recipe fields accepted when creating or replacing a recipe.</summary>
public sealed record RecipeRequest(
    Guid? AuthorId,
    Guid CategoryId,
    string Title,
    string? Slug,
    string? Description,
    string Content,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    DifficultyLevel Difficulty,
    NutritionRequest? Nutrition,
    IEnumerable<IngredientRequest>? Ingredients,
    IEnumerable<ImageRequest>? Images,
    IEnumerable<StepRequest>? Steps = null);
