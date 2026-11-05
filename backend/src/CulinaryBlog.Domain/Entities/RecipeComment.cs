using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Domain.Entities;

public class RecipeComment : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    public RecipeComment? ParentComment { get; set; }
    public ICollection<RecipeComment> Replies { get; set; } = new List<RecipeComment>();
    public string Content { get; set; } = string.Empty;
    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;
}
