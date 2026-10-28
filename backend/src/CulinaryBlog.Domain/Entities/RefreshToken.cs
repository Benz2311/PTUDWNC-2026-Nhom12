namespace CulinaryBlog.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public string Token
    {
        get => TokenHash;
        set => TokenHash = value ?? string.Empty;
    }

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked { get; set; }

    public string? ReplacedByTokenHash { get; set; }
    public string? ReplacedByToken
    {
        get => ReplacedByTokenHash;
        set => ReplacedByTokenHash = value;
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public byte[] RowVersion { get; set; } = new byte[16];
}
