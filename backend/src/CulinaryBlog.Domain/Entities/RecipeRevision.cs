namespace CulinaryBlog.Domain.Entities;

public class RecipeRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentJson { get; set; } = "{}";
    public Guid EditorId { get; set; }
    public ApplicationUser Editor { get; set; } = null!;
    public string ChangeSummary { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
