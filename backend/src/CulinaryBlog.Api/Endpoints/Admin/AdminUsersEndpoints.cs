using System.Security.Claims;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Repositories;
using CulinaryBlog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Endpoints;

public static class AdminUsersEndpoints
{
    public static void MapAdminUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/users")
            .WithTags("AdminUsers")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        // GET /api/v1/admin/users
        group.MapGet("/", async (
            IUserRepository userRepository,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null,
            [FromQuery] bool? isActive = null,
            CancellationToken cancellationToken = default) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, totalCount) = await userRepository.GetUsersAsync(
                page,
                pageSize,
                keyword,
                isActive,
                cancellationToken);
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
            var meta = new PaginationMeta(
                page,
                pageSize,
                totalCount,
                totalPages,
                page < totalPages,
                page > 1 && page <= totalPages);

            return Results.Ok(ApiResponse.Paged(items, meta));
        })
        .WithName("GetAdminUsers")
        .WithSummary("Lấy danh sách người dùng cho Admin (phân trang, tìm kiếm, lọc)")
        .Produces<ApiResponse<IReadOnlyList<UserResponse>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // PATCH /api/v1/admin/users/{id:guid}/status
        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateUserStatusRequest request,
            ClaimsPrincipal userPrincipal,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var adminUserIdClaim = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? userPrincipal.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(adminUserIdClaim) || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
            {
                return Results.Unauthorized();
            }

            await authService.UpdateUserStatusAsync(adminUserId, id, request.IsActive);
            return Results.NoContent();
        })
        .WithName("UpdateUserStatus")
        .WithSummary("Admin thay đổi trạng thái kích hoạt tài khoản người dùng")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
