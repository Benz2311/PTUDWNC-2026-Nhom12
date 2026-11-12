using CulinaryBlog.Application.Features.CommentReports.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.CommentReports.Queries.GetCommentReports;

public record GetCommentReportsQuery(
    string? StatusFilter = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<CommentReportPagedResultDto>;

public class GetCommentReportsQueryHandler : IRequestHandler<GetCommentReportsQuery, CommentReportPagedResultDto>
{
    private readonly IApplicationDbContext _context;

    public GetCommentReportsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CommentReportPagedResultDto> Handle(GetCommentReportsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = _context.CommentReports
            .AsNoTracking()
            .Include(r => r.Comment)
                .ThenInclude(c => c.Recipe)
            .Include(r => r.Comment)
                .ThenInclude(c => c.User)
            .Include(r => r.Reporter)
            .Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.StatusFilter) && !request.StatusFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<ReportStatus>(request.StatusFilter, true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CommentReportDto
            {
                Id = r.Id,
                CommentId = r.CommentId,
                CommentContent = r.Comment != null ? r.Comment.Content : string.Empty,
                CommentAuthorId = r.Comment != null ? r.Comment.UserId : Guid.Empty,
                CommentAuthorName = r.Comment != null && r.Comment.User != null ? r.Comment.User.DisplayName : "Người dùng",
                RecipeId = r.Comment != null ? r.Comment.RecipeId : Guid.Empty,
                RecipeTitle = r.Comment != null && r.Comment.Recipe != null ? r.Comment.Recipe.Title : string.Empty,
                RecipeSlug = r.Comment != null && r.Comment.Recipe != null ? r.Comment.Recipe.Slug : string.Empty,
                ReporterId = r.ReporterId,
                ReporterName = r.Reporter != null ? r.Reporter.DisplayName : "Người dùng",
                Reason = r.Reason,
                Details = r.Details,
                Status = r.Status.ToString(),
                IsCommentHidden = r.Comment != null && r.Comment.IsHidden,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt,
                ResolutionNotes = r.ResolutionNotes
            })
            .ToListAsync(cancellationToken);

        return new CommentReportPagedResultDto
        {
            Items = reports,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1 && page <= totalPages
        };
    }
}
