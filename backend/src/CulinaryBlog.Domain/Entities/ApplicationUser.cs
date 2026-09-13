using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

public class ApplicationUser : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PasswordHash { get; set; }
    public bool EmailConfirmed { get; set; }
    public ICollection<string> Roles { get; set; } = new List<string>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
