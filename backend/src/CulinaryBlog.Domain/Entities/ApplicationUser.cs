using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string FullName
    {
        get => DisplayName;
        set => DisplayName = value;
    }

    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }

    public string[] Roles { get; set; } = ["Author"];

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();

    public ICollection<Recipe> Recipes { get; set; }
        = new List<Recipe>();
}