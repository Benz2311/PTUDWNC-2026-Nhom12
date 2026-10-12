namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>A reference to a recipe returned after a create or update operation.</summary>
public sealed record RecipeReferenceResponse(Guid Id, string Slug);
