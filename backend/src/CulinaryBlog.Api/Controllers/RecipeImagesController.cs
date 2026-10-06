using System.Security.Claims;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Features.Recipes.Queries.RecipeImages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Controllers;

[ApiController]
[Route("api/v1/recipes/{recipeId:guid}/images")]
public class RecipeImagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecipeImagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách hình ảnh của công thức
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecipeImageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImages(Guid recipeId, CancellationToken cancellationToken)
    {
        var query = new GetRecipeImagesQuery(recipeId);
        var images = await _mediator.Send(query, cancellationToken);
        return Ok(images);
    }

    /// <summary>
    /// Lấy chi tiết 1 hình ảnh
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RecipeImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(Guid recipeId, Guid id, CancellationToken cancellationToken)
    {
        var query = new GetRecipeImageByIdQuery(id, recipeId);
        var image = await _mediator.Send(query, cancellationToken);
        return Ok(image);
    }

    /// <summary>
    /// Thêm hình ảnh mới cho công thức (Yêu cầu đăng nhập)
    /// </summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(RecipeImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddImage(
        Guid recipeId,
        [FromBody] AddRecipeImageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");

        var command = new AddRecipeImageCommand(
            recipeId,
            request.OriginalUrl,
            request.MediumUrl,
            request.ThumbnailUrl,
            request.AltText,
            request.IsPrimary,
            request.OrderIndex,
            userId,
            isAdmin);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetImage), new { recipeId, id = result.Id }, result);
    }

    /// <summary>
    /// Cập nhật thông tin hình ảnh / Đặt làm ảnh đại diện (Yêu cầu đăng nhập)
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RecipeImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateImage(
        Guid recipeId,
        Guid id,
        [FromBody] UpdateRecipeImageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");

        // Nếu chỉ cập nhật IsPrimary, dùng SetPrimaryRecipeImageCommand
        if (request.IsPrimary.HasValue && request.IsPrimary.Value &&
            string.IsNullOrWhiteSpace(request.AltText) &&
            request.OriginalUrl is null &&
            request.MediumUrl is null &&
            request.ThumbnailUrl is null &&
            request.OrderIndex is null)
        {
            var setPrimaryCommand = new SetPrimaryRecipeImageCommand(recipeId, id, userId, isAdmin);
            var result = await _mediator.Send(setPrimaryCommand, cancellationToken);
            return Ok(result);
        }

        // TODO: Implement UpdateRecipeImageCommand for other fields
        // For now, return 400 for other updates
        return BadRequest(new { message = "Only IsPrimary update is supported currently. Use PUT /{id}/set-primary for setting primary image." });
    }

    /// <summary>
    /// Đặt hình ảnh làm ảnh đại diện (Primary)
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}/set-primary")]
    [ProducesResponseType(typeof(RecipeImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(Guid recipeId, Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");

        var command = new SetPrimaryRecipeImageCommand(recipeId, id, userId, isAdmin);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xóa hình ảnh (Yêu cầu đăng nhập)
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(Guid recipeId, Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");

        var command = new DeleteRecipeImageCommand(recipeId, id, userId, isAdmin);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user identity.");
        }

        return userId;
    }
}

// Request DTOs
public record AddRecipeImageRequest(
    string OriginalUrl,
    string? MediumUrl = null,
    string? ThumbnailUrl = null,
    string? AltText = null,
    bool? IsPrimary = null,
    int? OrderIndex = null);

public record UpdateRecipeImageRequest(
    string? OriginalUrl = null,
    string? MediumUrl = null,
    string? ThumbnailUrl = null,
    string? AltText = null,
    bool? IsPrimary = null,
    int? OrderIndex = null);