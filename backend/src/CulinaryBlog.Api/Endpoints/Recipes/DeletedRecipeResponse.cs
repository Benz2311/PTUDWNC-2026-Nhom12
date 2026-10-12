namespace CulinaryBlog.Api.Endpoints.Recipes;

public sealed record DeletedRecipeResponse(
    Guid Id,
    string Title,
    string Slug,
    string Status,
    DateTime DeletedAt);
