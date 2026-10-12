namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>A recipe with its ingredients, preparation steps, nutrition, and images.</summary>
public sealed record RecipeDetailResponse(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string Content,
    Guid CategoryId,
    string Category,
    string Author,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string Difficulty,
    string Status,
    DateTime CreatedAt,
    IEnumerable<IngredientResponse> Ingredients,
    IEnumerable<StepResponse> Steps,
    NutritionResponse? Nutrition,
    IEnumerable<ImageResponse> Images);
