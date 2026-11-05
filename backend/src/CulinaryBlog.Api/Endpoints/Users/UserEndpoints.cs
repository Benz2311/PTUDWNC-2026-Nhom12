using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/users")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Users");

        group.MapGet("/", GetUsers);
        group.MapPatch("/{id:guid}/status", ToggleUserStatus);
        group.MapPatch("/{id:guid}/roles", UpdateUserRoles);
    }

    private static async Task<IResult> GetUsers(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto(
                u.Id,
                u.UserName,
                u.DisplayName,
                u.Email,
                u.AvatarUrl,
                u.Roles,
                u.IsActive,
                u.CreatedAt,
                db.Recipes.Count(r => r.AuthorId == u.Id && !r.IsDeleted)))
            .ToListAsync(cancellationToken);

        return Results.Ok(users);
    }

    private static async Task<IResult> ToggleUserStatus(
        Guid id,
        [FromBody] ToggleStatusRequest request,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return Results.NotFound(new { message = "Không tìm thấy người dùng." });
        }

        user.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Cập nhật trạng thái thành công.", isActive = user.IsActive });
    }

    private static async Task<IResult> UpdateUserRoles(
        Guid id,
        [FromBody] UpdateRolesRequest request,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return Results.NotFound(new { message = "Không tìm thấy người dùng." });
        }

        if (request.Roles is null || request.Roles.Length == 0)
        {
            return Results.BadRequest(new { message = "Người dùng phải có ít nhất một vai trò." });
        }

        user.Roles = request.Roles;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Cập nhật vai trò thành công.", roles = user.Roles });
    }
}

public sealed record AdminUserDto(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    string[] Roles,
    bool IsActive,
    DateTime CreatedAt,
    int RecipeCount);

public sealed record ToggleStatusRequest(bool IsActive);

public sealed record UpdateRolesRequest(string[] Roles);

