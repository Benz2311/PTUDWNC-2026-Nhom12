namespace CulinaryBlog.Domain.Entities;

public class RecipeStep
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RecipeId { get; set; }

    public Recipe Recipe { get; set; } = null!;

    public int StepNumber { get; set; }

    public string? Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public int? TimerMinutes { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public byte[] RowVersion { get; set; } = new byte[8];
}