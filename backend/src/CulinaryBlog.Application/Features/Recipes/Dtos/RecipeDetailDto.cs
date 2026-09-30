using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Features.Recipes.Dtos;

public class RecipeDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public RecipeStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public AuthorSummaryDto Author { get; set; } = null!;
    public CategorySummaryDto Category { get; set; } = null!;
    public RecipeNutritionDto? Nutrition { get; set; }

    public List<RecipeStepDto> Steps { get; set; } = new();
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
    public List<RecipeImageDto> Images { get; set; } = new();
}

public class AuthorSummaryDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class CategorySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class RecipeNutritionDto
{
    public decimal? Calories { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Carbohydrates { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Fiber { get; set; }
    public decimal? Sodium { get; set; }
}

public class RecipeStepDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? TimerMinutes { get; set; }
    public string? ImageUrl { get; set; }
}

public class RecipeIngredientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

public class RecipeImageDto
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string? MediumUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int OrderIndex { get; set; }
}
