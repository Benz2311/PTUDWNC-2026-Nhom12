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

    public void Validate()
    {
        if (StepNumber < 1)
        {
            throw new Exceptions.InvalidRecipeStepException($"Step number must be >= 1. Actual: {StepNumber}");
        }

        if (TimerMinutes.HasValue && TimerMinutes.Value < 0)
        {
            throw new Exceptions.InvalidRecipeStepException($"Timer minutes cannot be negative. Actual: {TimerMinutes.Value}");
        }
    }
}