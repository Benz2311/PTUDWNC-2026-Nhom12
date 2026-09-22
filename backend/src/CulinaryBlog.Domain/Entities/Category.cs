using CulinaryBlog.Domain.Common;

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
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
        Description = description;
        ImageUrl = imageUrl;
        OrderIndex = orderIndex;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public static Category Create(
        string name,
        string? slug = null,
        string? description = null,
        string? imageUrl = null,
        int orderIndex = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var generatedSlug = string.IsNullOrWhiteSpace(slug)
            ? SlugHelper.Generate(name)
            : slug.Trim().ToLowerInvariant();

        return new Category(
            name.Trim(),
            generatedSlug,
            description?.Trim(),
            imageUrl?.Trim(),
            orderIndex);
    }

    public void Update(
        string name,
        string? description = null,
        string? imageUrl = null,
        int orderIndex = 0,
        string? slug = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        ImageUrl = imageUrl?.Trim();
        OrderIndex = orderIndex;

        if (!string.IsNullOrWhiteSpace(slug))
        {
            Slug = slug.Trim().ToLowerInvariant();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRecipe(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (!_recipes.Contains(recipe))
        {
            _recipes.Add(recipe);
        }
    }
}