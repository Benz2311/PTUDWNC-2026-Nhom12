namespace CulinaryBlog.Domain.Entities;

public class RecipeIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public int SortOrder { get; set; }
}
