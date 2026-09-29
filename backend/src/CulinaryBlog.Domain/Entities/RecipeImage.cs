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
}