using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.CommentReports.Commands.CreateCommentReport;

public record CreateCommentReportCommand(
    Guid CommentId,
    Guid UserId,
    string Reason,
    string? Details = null
) : IRequest<Guid>;

public class CreateCommentReportCommandHandler : IRequestHandler<CreateCommentReportCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateCommentReportCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCommentReportCommand request, CancellationToken cancellationToken)
    {
        var trimmedReason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedReason))
        {
            throw new ValidationException("Lý do báo cáo không được để trống.");
        }

        var comment = await _context.RecipeComments
            .AsNoTracking()
            .Where(c => c.Id == request.CommentId && !c.IsDeleted)
            .Select(c => new { c.Id, c.UserId, c.IsHidden })
            .FirstOrDefaultAsync(cancellationToken);

        if (comment == null)
        {
            throw new NotFoundException($"Bình luận '{request.CommentId}' không tồn tại hoặc đã bị xóa.");
        }

        if (comment.UserId == request.UserId)
        {
            throw new ValidationException("Bạn không thể tự báo cáo bình luận của chính mình.");
        }

        // Kiểm tra chống báo cáo trùng lặp: nếu đã có báo cáo đang Pending từ người này
        var existingPendingReport = await _context.CommentReports
            .AnyAsync(r => r.CommentId == request.CommentId
                        && r.ReporterId == request.UserId
                        && r.Status == ReportStatus.Pending
                        && !r.IsDeleted,
                cancellationToken);

        if (existingPendingReport)
        {
            throw new ValidationException("Bạn đã gửi báo cáo cho bình luận này và đang được chờ quản trị viên xử lý.");
        }

        var report = new CommentReport(
            request.CommentId,
            request.UserId,
            trimmedReason,
            request.Details?.Trim());

        await _context.CommentReports.AddAsync(report, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return report.Id;
    }
}
