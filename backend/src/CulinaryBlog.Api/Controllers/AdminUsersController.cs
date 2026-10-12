using System.Security.Claims;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "RequireAdmin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public AdminUsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _authService.GetUsersAsync(page, pageSize, keyword, isActive);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var response = new PagedUsersResponse(
            items.Select(u => new AdminUserDto(
                u.Id.ToString(),
                u.UserName,
                u.Email,
                u.DisplayName,
                u.AvatarUrl,
                u.Bio,
                u.Roles,
                u.IsActive,
                u.CreatedAt.ToString("o")
            )).ToList(),
            page,
            pageSize,
            totalCount,
            totalPages
        );

        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(adminUserIdClaim) || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized();
        }

        await _authService.UpdateUserStatusAsync(adminUserId, id, request.IsActive);
        return NoContent();
    }
}

public record AdminUserDto(
    string Id,
    string UserName,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    string[] Roles,
    bool IsActive,
    string CreatedAt);

public record PagedUsersResponse(
    IReadOnlyList<AdminUserDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);