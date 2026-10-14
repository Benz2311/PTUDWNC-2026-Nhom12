namespace CulinaryBlog.Api.Endpoints.Recipes;

/// <summary>A compact recipe representation used by recipe listing endpoints.</summary>
public sealed record RecipeSummaryResponse(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string Category,
    string Author,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string Difficulty,
    string Status,
    DateTime CreatedAt,
    DateTimeOffset? PublishedAt,
    string? PrimaryImageUrl);
