namespace CulinaryBlog.Application.Features.CommentReports.Dtos;

public class CommentReportDto
{
    public Guid Id { get; set; }
    public Guid CommentId { get; set; }
    public string CommentContent { get; set; } = string.Empty;
    public Guid CommentAuthorId { get; set; }
    public string CommentAuthorName { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
    public string RecipeTitle { get; set; } = string.Empty;
    public string RecipeSlug { get; set; } = string.Empty;
    public Guid ReporterId { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsCommentHidden { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class CommentReportPagedResultDto
{
    public IReadOnlyList<CommentReportDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
