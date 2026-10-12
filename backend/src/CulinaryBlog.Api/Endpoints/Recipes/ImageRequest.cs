namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>An image URL and optional alternative text supplied for a recipe.</summary>
public sealed record ImageRequest(string Url, string? AltText);
