using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace CulinaryBlog.Domain.Entities;

public class Recipe : BaseEntity
{
    public Recipe()
    {
    }

    public Recipe(
        string title,
        string slug,
        string? description,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        DifficultyLevel difficulty,
        RecipeStatus status,
        Guid categoryId)
    {
        Title = title;
        Slug = slug;
        Description = description ?? string.Empty;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Servings = servings;
        Difficulty = difficulty;
        Status = status;
        CategoryId = categoryId;
    }

    public Guid AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; } = 1;

    [NotMapped]
    public string? SearchVector { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DifficultyLevel Difficulty { get; set; }

    public RecipeStatus Status { get; set; } = RecipeStatus.Draft;

    public ICollection<RecipeIngredient> Ingredients { get; set; }
        = new List<RecipeIngredient>();

    public ICollection<RecipeStep> Steps { get; set; }
        = new List<RecipeStep>();

    public ICollection<RecipeImage> Images { get; set; }
        = new List<RecipeImage>();

    public RecipeNutrition Nutrition { get; set; } = new();
}