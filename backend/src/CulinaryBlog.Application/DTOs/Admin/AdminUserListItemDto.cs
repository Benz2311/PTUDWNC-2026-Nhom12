namespace CulinaryBlog.Application.DTOs.Admin;

/// <summary>Represents a user row in the administrator's user list.</summary>
public sealed record AdminUserListItemDto(
    string Id,
    string UserName,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    string[] Roles,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int RecipeCount);

/// <summary>Contains the roles to assign to an account.</summary>
public sealed record UpdateUserRolesRequest(string[] Roles);