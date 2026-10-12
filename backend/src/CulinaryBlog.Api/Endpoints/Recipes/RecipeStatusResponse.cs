namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>The result of changing or restoring a recipe's lifecycle status.</summary>
public sealed record RecipeStatusResponse(Guid Id, string Status);
