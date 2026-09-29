namespace CulinaryBlog.Domain.Entities;

public class RecipeImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RecipeId { get; set; }

    public Recipe Recipe { get; set; } = null!;

    public string OriginalUrl { get; set; } = string.Empty;

    public string? MediumUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? AltText { get; set; }

    public bool IsPrimary { get; set; }

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public byte[] RowVersion { get; set; } = new byte[8];

    public void Validate()
    {
        if (OrderIndex < 0)
        {
            throw new Exceptions.InvalidRecipeImageException($"Order index cannot be negative. Actual: {OrderIndex}");
        }

        if (string.IsNullOrWhiteSpace(OriginalUrl))
        {
            throw new Exceptions.InvalidRecipeImageException("OriginalUrl cannot be empty.");
        }
    }
}