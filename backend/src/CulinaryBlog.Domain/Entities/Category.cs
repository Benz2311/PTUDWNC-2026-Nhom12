namespace CulinaryBlog.Domain.Entities;

public class Category : BaseEntity
{
    private readonly List<Recipe> _recipes = new();

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? ImageUrl { get; private set; }

    public int OrderIndex { get; private set; }

    public IReadOnlyCollection<Recipe> Recipes => _recipes.AsReadOnly();

    private Category()
    {
    }

    private Category(
        string name,
        string slug,
        string? description,
        string? imageUrl,
        int orderIndex)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ImageUrl = imageUrl;
        OrderIndex = orderIndex;
    }

    public static Category Create(
        string name,
        string slug,
        string? description = null,
        string? imageUrl = null,
        int orderIndex = 0)
    {
        return new Category(
            name,
            slug,
            description,
            imageUrl,
            orderIndex);
    }

    public void Update(
        string name,
        string? description,
        string? imageUrl,
        int orderIndex)
    {
        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        OrderIndex = orderIndex;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}