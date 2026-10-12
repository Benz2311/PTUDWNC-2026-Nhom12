namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>A preparation step supplied when creating or updating a recipe.</summary>
public sealed record StepRequest(string? Title, string Description);
