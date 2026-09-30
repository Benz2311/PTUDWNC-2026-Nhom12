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

    public int SortOrder => OrderIndex;

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
        string? slug = null,
        string? description = null,
        string? imageUrl = null,
        int orderIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }

        var finalSlug = string.IsNullOrWhiteSpace(slug) ? SlugHelper.Generate(name) : slug;

        return new Category(
            name,
            finalSlug,
            description,
            imageUrl,
            orderIndex);
    }

    public void Update(
        string name,
        string? description = null,
        string? imageUrl = null,
        int? orderIndex = null,
        string? slug = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }

        Name = name;

        if (slug != null)
        {
            Slug = slug;
        }

        if (description != null)
        {
            Description = description;
        }

        if (imageUrl != null)
        {
            ImageUrl = imageUrl;
        }

        if (orderIndex.HasValue)
        {
            OrderIndex = orderIndex.Value;
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