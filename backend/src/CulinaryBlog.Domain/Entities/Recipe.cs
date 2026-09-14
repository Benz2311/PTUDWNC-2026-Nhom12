namespace CulinaryBlog.Domain.Entities;

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}

public enum RecipeStatus
{
    Draft,
    Published,
    Archived
}

public class Recipe : BaseEntity
{
    public string Title { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int PrepTimeMinutes { get; private set; }

    public int CookTimeMinutes { get; private set; }

    public int Servings { get; private set; }

    public DifficultyLevel Difficulty { get; private set; }

    public RecipeStatus Status { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    private Recipe()
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
        Id = Guid.NewGuid();
        Title = title;
        Slug = slug;
        Description = description;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Servings = servings;
        Difficulty = difficulty;
        Status = status;
        CategoryId = categoryId;
        CreatedAt = DateTime.UtcNow;
    }
}