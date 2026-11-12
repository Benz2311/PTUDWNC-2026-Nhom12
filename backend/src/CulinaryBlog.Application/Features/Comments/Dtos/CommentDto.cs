namespace CulinaryBlog.Application.Features.Comments.Dtos;

public class CommentAuthorDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? ParentCommentId { get; set; }
    public bool IsHidden { get; set; }
    public CommentAuthorDto Author { get; set; } = null!;
    public List<CommentDto> Replies { get; set; } = new();
}

public class CommentPagedResultDto
{
    public IReadOnlyList<CommentDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
