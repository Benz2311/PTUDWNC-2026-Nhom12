using System.Security.Claims;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Features.Comments.Commands.CreateComment;
using CulinaryBlog.Application.Features.Comments.Commands.DeleteComment;
using CulinaryBlog.Application.Features.Comments.Commands.UpdateComment;
using CulinaryBlog.Application.Features.Comments.Dtos;
using CulinaryBlog.Application.Features.Comments.Queries.GetRecipeComments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Endpoints;

public record CreateCommentRequest(string Content, Guid? ParentCommentId = null);
public record UpdateCommentRequest(string Content);

public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this WebApplication app)
    {
        // 1. Group recipes/{recipeId}/comments
        var recipeCommentsGroup = app.MapGroup("/api/v1/recipes/{recipeId:guid}/comments")
            .WithTags("Comments")
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        // GET /api/v1/recipes/{recipeId:guid}/comments
        recipeCommentsGroup.MapGet("/", async (
            Guid recipeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            ClaimsPrincipal? user = null,
            ISender? sender = null,
            CancellationToken ct = default) =>
        {
            bool isAdmin = user?.IsInRole("Admin") ?? false;
            var result = await sender!.Send(new GetRecipeCommentsQuery(recipeId, page, pageSize, isAdmin), ct);

            var meta = new PaginationMeta(
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages,
                result.HasNextPage,
                result.HasPreviousPage);

            return Results.Ok(ApiResponse.Paged(result.Items, meta));
        })
        .WithName("GetRecipeComments")
        .WithSummary("Lấy danh sách bình luận của công thức (phân trang, hỗ trợ trả lời 1 cấp)")
        .Produces<ApiResponse<IReadOnlyList<CommentDto>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /api/v1/recipes/{recipeId:guid}/comments
        recipeCommentsGroup.MapPost("/", async (
            Guid recipeId,
            [FromBody] CreateCommentRequest request,
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

            var result = await sender.Send(new CreateCommentCommand(
                recipeId,
                userId,
                request.Content,
                request.ParentCommentId), ct);

            return Results.Created($"/api/v1/comments/{result.Id}", ApiResponse.Success(result));
        })
        .RequireAuthorization()
        .WithName("CreateRecipeComment")
        .WithSummary("Thêm bình luận mới hoặc trả lời bình luận (1 cấp) dưới công thức Published")
        .Produces<ApiResponse<CommentDto>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // 2. Group comments/{commentId}
        var commentsGroup = app.MapGroup("/api/v1/comments/{commentId:guid}")
            .WithTags("Comments")
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        // PUT /api/v1/comments/{commentId:guid}
        commentsGroup.MapPut("/", async (
            Guid commentId,
            [FromBody] UpdateCommentRequest request,
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

            var result = await sender.Send(new UpdateCommentCommand(commentId, userId, request.Content), ct);
            return Results.Ok(ApiResponse.Success(result));
        })
        .RequireAuthorization()
        .WithName("UpdateComment")
        .WithSummary("Chỉnh sửa nội dung bình luận (chỉ tác giả bình luận có quyền sửa)")
        .Produces<ApiResponse<CommentDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // DELETE /api/v1/comments/{commentId:guid}
        commentsGroup.MapDelete("/", async (
            Guid commentId,
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

            bool isAdmin = user.IsInRole("Admin");
            await sender.Send(new DeleteCommentCommand(commentId, userId, isAdmin), ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("DeleteComment")
        .WithSummary("Xóa bình luận (tác giả hoặc Admin có quyền xóa)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
