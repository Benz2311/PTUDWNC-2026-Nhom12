using System.Security.Claims;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Features.CommentReports.Commands.CreateCommentReport;
using CulinaryBlog.Application.Features.CommentReports.Commands.ResolveCommentReport;
using CulinaryBlog.Application.Features.CommentReports.Dtos;
using CulinaryBlog.Application.Features.CommentReports.Queries.GetCommentReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Endpoints;

public record CreateCommentReportRequest(string Reason, string? Details = null);
public record ResolveCommentReportRequest(string Action, string? ResolutionNotes = null);

public static class CommentReportEndpoints
{
    public static void MapCommentReportEndpoints(this WebApplication app)
    {
        // 1. User reporting endpoint: POST /api/v1/comments/{commentId:guid}/reports
        app.MapPost("/api/v1/comments/{commentId:guid}/reports", async (
            Guid commentId,
            [FromBody] CreateCommentReportRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var reportId = await sender.Send(new CreateCommentReportCommand(
                commentId,
                userId,
                request.Reason,
                request.Details), ct);

            return Results.Created($"/api/v1/comments/{commentId}/reports/{reportId}", ApiResponse.Success(new { ReportId = reportId }));
        })
        .RequireAuthorization()
        .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy)
        .WithName("ReportComment")
        .WithSummary("Báo cáo bình luận vi phạm tiêu chuẩn cộng đồng (có cơ chế hạn chế trùng lặp)")
        .Produces<ApiResponse<object>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // 2. Admin comment reports management endpoints: /api/v1/admin/comment-reports
        var adminGroup = app.MapGroup("/api/v1/admin/comment-reports")
            .WithTags("AdminCommentReports")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        // GET /api/v1/admin/comment-reports
        adminGroup.MapGet("/", async (
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            ISender? sender = null,
            CancellationToken ct = default) =>
        {
            var result = await sender!.Send(new GetCommentReportsQuery(status, page, pageSize), ct);

            var meta = new PaginationMeta(
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages,
                result.HasNextPage,
                result.HasPreviousPage);

            return Results.Ok(ApiResponse.Paged(result.Items, meta));
        })
        .WithName("GetAdminCommentReports")
        .WithSummary("Lấy danh sách báo cáo bình luận cho Admin (lọc theo trạng thái, phân trang)")
        .Produces<ApiResponse<IReadOnlyList<CommentReportDto>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // PUT /api/v1/admin/comment-reports/{reportId:guid}/resolve
        adminGroup.MapPut("/{reportId:guid}/resolve", async (
            Guid reportId,
            [FromBody] ResolveCommentReportRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdClaim, out var adminId))
            {
                return Results.Unauthorized();
            }

            await sender.Send(new ResolveCommentReportCommand(
                reportId,
                adminId,
                request.Action,
                request.ResolutionNotes), ct);

            return Results.Ok(ApiResponse.Success(new { Message = "Đã xử lý báo cáo thành công." }));
        })
        .WithName("ResolveAdminCommentReport")
        .WithSummary("Admin xử lý báo cáo bình luận: 'Dismiss' (giữ lại) hoặc 'HideComment' (ẩn bình luận vi phạm)")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
