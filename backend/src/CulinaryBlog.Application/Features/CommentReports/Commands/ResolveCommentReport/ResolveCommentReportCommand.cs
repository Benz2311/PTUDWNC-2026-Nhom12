using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.CommentReports.Commands.ResolveCommentReport;

public record ResolveCommentReportCommand(
    Guid ReportId,
    Guid AdminId,
    string Action, // "Dismiss" or "HideComment"
    string? ResolutionNotes = null
) : IRequest<bool>;

public class ResolveCommentReportCommandHandler : IRequestHandler<ResolveCommentReportCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ResolveCommentReportCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ResolveCommentReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.CommentReports
            .Include(r => r.Comment)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId && !r.IsDeleted, cancellationToken);

        if (report == null)
        {
            throw new NotFoundException($"Báo cáo '{request.ReportId}' không tồn tại.");
        }

        var normalizedAction = request.Action?.Trim().ToLowerInvariant();
        if (normalizedAction != "dismiss" && normalizedAction != "hidecomment")
        {
            throw new ValidationException("Hành động xử lý không hợp lệ. Chỉ chấp nhận 'Dismiss' hoặc 'HideComment'.");
        }

        if (normalizedAction == "hidecomment")
        {
            if (report.Comment != null)
            {
                report.Comment.IsHidden = true;
                report.Comment.HiddenAt = DateTime.UtcNow;
                report.Comment.HiddenByUserId = request.AdminId;
                report.Comment.UpdatedAt = DateTime.UtcNow;
            }

            report.Status = ReportStatus.ActionTaken;

            // Đánh dấu các báo cáo đang Pending khác của bình luận này cũng thành ActionTaken
            var otherPendingReports = await _context.CommentReports
                .Where(r => r.CommentId == report.CommentId && r.Id != report.Id && r.Status == ReportStatus.Pending && !r.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var other in otherPendingReports)
            {
                other.Status = ReportStatus.ActionTaken;
                other.ResolvedAt = DateTime.UtcNow;
                other.ResolvedByUserId = request.AdminId;
                other.ResolutionNotes = $"Tự động giải quyết theo báo cáo {report.Id}";
            }
        }
        else
        {
            report.Status = ReportStatus.Dismissed;
        }

        report.ResolvedAt = DateTime.UtcNow;
        report.ResolvedByUserId = request.AdminId;
        report.ResolutionNotes = request.ResolutionNotes?.Trim();
        report.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
