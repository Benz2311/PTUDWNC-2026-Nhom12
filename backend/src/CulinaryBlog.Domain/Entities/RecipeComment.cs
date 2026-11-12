using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

public class RecipeComment : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public Guid? ParentCommentId { get; set; }
    public RecipeComment? ParentComment { get; set; }
    public ICollection<RecipeComment> Replies { get; set; } = new List<RecipeComment>();

    public bool IsHidden { get; set; }
    public DateTime? HiddenAt { get; set; }
    public Guid? HiddenByUserId { get; set; }

    public ICollection<CommentReport> Reports { get; set; } = new List<CommentReport>();

    public RecipeComment()
    {
    }

    public RecipeComment(Guid recipeId, Guid userId, string content, Guid? parentCommentId = null)
    {
        RecipeId = recipeId;
        UserId = userId;
        Content = content;
        ParentCommentId = parentCommentId;
    }
}
