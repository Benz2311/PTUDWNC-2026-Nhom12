namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>An ingredient included in a recipe.</summary>
public sealed record IngredientResponse(
    Guid Id,
    string Name,
    decimal? Quantity,
    string? Unit,
    int SortOrder,
    string? Notes);
