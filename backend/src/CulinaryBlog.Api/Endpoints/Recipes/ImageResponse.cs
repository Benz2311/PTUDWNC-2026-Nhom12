namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>An image associated with a recipe.</summary>
public sealed record ImageResponse(
    Guid Id,
    string Url,
    string? AltText,
    bool IsPrimary,
    int SortOrder);
