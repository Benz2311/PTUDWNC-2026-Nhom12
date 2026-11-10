namespace CulinaryBlog.Domain.Entities;

public class RecipeView
{
    public long Id { get; set; }
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}

