namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>Nutritional values for one recipe serving.</summary>
public sealed record NutritionResponse(
    decimal? Calories,
    decimal? Protein,
    decimal? Carbohydrates,
    decimal? Fat,
    decimal? Fiber,
    decimal? Sodium);
