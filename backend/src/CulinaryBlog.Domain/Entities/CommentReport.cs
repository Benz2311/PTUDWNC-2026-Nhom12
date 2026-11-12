using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Domain.Entities;

public class CommentReport : BaseEntity
{
    public Guid CommentId { get; set; }
    public RecipeComment Comment { get; set; } = null!;

    public Guid ReporterId { get; set; }
    public ApplicationUser Reporter { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public ApplicationUser? ResolvedByUser { get; set; }
    public string? ResolutionNotes { get; set; }

    public CommentReport()
    {
    }

    public CommentReport(Guid commentId, Guid reporterId, string reason, string? details = null)
    {
        CommentId = commentId;
        ReporterId = reporterId;
        Reason = reason;
        Details = details;
        Status = ReportStatus.Pending;
    }
}
