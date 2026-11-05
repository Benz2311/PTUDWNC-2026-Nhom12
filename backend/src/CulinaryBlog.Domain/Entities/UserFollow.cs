namespace CulinaryBlog.Domain.Entities;

public class UserFollow
{
    public Guid FollowerId { get; set; }
    public ApplicationUser Follower { get; set; } = null!;
    public Guid FollowingId { get; set; }
    public ApplicationUser Following { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
