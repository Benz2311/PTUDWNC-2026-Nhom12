using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Domain.Entities;

public class ModerationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public Guid ModeratorId { get; set; }
    public ApplicationUser Moderator { get; set; } = null!;
    public ModerationStatus Action { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
