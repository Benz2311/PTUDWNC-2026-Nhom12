namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>Nutritional values supplied for one recipe serving.</summary>
public sealed record NutritionRequest(
    decimal Calories,
    decimal Protein,
    decimal Carbohydrates,
    decimal Fat,
    decimal Fiber,
    decimal Sodium = 0);
