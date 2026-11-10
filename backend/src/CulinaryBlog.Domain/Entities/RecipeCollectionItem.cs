namespace CulinaryBlog.Domain.Entities;

public class RecipeCollectionItem
{
    public Guid CollectionId { get; set; }
    public RecipeCollection Collection { get; set; } = null!;

    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

