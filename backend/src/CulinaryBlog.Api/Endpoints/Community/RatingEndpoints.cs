using System.Security.Claims;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Features.Ratings.Commands.DeleteRating;
using CulinaryBlog.Application.Features.Ratings.Commands.UpsertRating;
using CulinaryBlog.Application.Features.Ratings.Dtos;
using CulinaryBlog.Application.Features.Ratings.Queries.GetRecipeRatingSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Endpoints;

public record UpsertRatingRequest(int Score);

public static class RatingEndpoints
{
    public static void MapRatingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/recipes/{recipeId:guid}/ratings")
            .WithTags("Ratings")
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        // GET /api/v1/recipes/{recipeId:guid}/ratings/summary
        group.MapGet("/summary", async (
            Guid recipeId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            Guid? currentUserId = null;
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                currentUserId = parsedUserId;
            }

            var result = await sender.Send(new GetRecipeRatingSummaryQuery(recipeId, currentUserId), ct);
            return Results.Ok(ApiResponse.Success(result));
        })
        .WithName("GetRecipeRatingSummary")
        .WithSummary("Lấy điểm đánh giá trung bình và tổng số lượt đánh giá của công thức")
        .Produces<ApiResponse<RecipeRatingSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /api/v1/recipes/{recipeId:guid}/ratings
        group.MapPost("/", async (
            Guid recipeId,
            [FromBody] UpsertRatingRequest request,
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

            var result = await sender.Send(new UpsertRatingCommand(recipeId, userId, request.Score), ct);
            return Results.Ok(ApiResponse.Success(result));
        })
        .RequireAuthorization()
        .WithName("UpsertRecipeRating")
        .WithSummary("Thêm hoặc cập nhật đánh giá công thức (1-5 sao)")
        .Produces<ApiResponse<RecipeRatingSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // DELETE /api/v1/recipes/{recipeId:guid}/ratings
        group.MapDelete("/", async (
            Guid recipeId,
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
            var result = await sender.Send(new DeleteRatingCommand(recipeId, userId, isAdmin), ct);
            return Results.Ok(ApiResponse.Success(result));
        })
        .RequireAuthorization()
        .WithName("DeleteRecipeRating")
        .WithSummary("Xóa đánh giá công thức của người dùng")
        .Produces<ApiResponse<RecipeRatingSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
