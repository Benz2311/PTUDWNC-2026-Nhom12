using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Features.Search.Dtos;

public class RecipeSearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public DateTime? PublishedAt { get; set; }

    public string? PrimaryImageUrl { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;

    public string MatchType { get; set; } = "FullTextSearch"; // "FullTextSearch" hoặc "FuzzyTrigram"
}
