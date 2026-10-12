using System.Text.Json;

namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>An ingredient supplied when creating or updating recipe contents.</summary>
public sealed record IngredientRequest(
    string Name,
    JsonElement Quantity,
    string? Unit,
    int SortOrder = 0,
    string? Notes = null,
    int? OrderIndex = null);
