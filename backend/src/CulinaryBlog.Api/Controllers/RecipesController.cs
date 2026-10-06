using System.Security.Claims;
using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Search.Dtos;
using CulinaryBlog.Application.Features.Search.Queries.SearchRecipes;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly ISender _sender;
    private readonly ICacheService _cache;

    public RecipesController(
        IRecipeService recipeService,
        ISender sender,
        ICacheService cache)
    {
        _recipeService = recipeService;
        _sender = sender;
        _cache = cache;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<RecipeListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] RecipeSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _recipeService.GetPagedListAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<RecipeSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var effectivePage = page > 0 ? page : 1;
        var effectivePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var normalizedQ = q?.Trim().ToLowerInvariant() ?? string.Empty;

        // SRS FR-SRCH-001: Cache Redis 1 phút vary theo query/page/pageSize
        var cacheKey = $"search:{normalizedQ}:{effectivePage}:{effectivePageSize}";
        var cachedResult = await _cache.GetAsync<PagedResult<RecipeSearchResultDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return Ok(cachedResult);
        }

        var result = await _sender.Send(
            new SearchRecipesQuery(q, effectivePage, effectivePageSize),
            cancellationToken);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(1), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(RecipeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var cacheKey = $"recipe:{normalizedSlug}";

        Guid? currentUserId = GetCurrentUserId();
        bool isAdmin = User.IsInRole("Admin");

        // Double-Shield Cache: chỉ phục vụ recipe Published từ shared cache
        var cachedRecipe = await _cache.GetAsync<RecipeDetailDto>(cacheKey, cancellationToken);
        if (cachedRecipe is { Status: RecipeStatus.Published })
        {
            return Ok(cachedRecipe);
        }

        var recipe = await _sender.Send(
            new GetRecipeBySlugQuery(slug, currentUserId, isAdmin),
            cancellationToken);

        // Major-01: CHỈ cache công thức có trạng thái Published
        if (recipe.Status == RecipeStatus.Published)
        {
            await _cache.SetAsync(cacheKey, recipe, TimeSpan.FromMinutes(5), cancellationToken);
        }

        return Ok(recipe);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(RecipeDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRecipeDto dto,
        CancellationToken cancellationToken)
    {
        var authorId = GetRequiredUserId();
        var recipe = await _recipeService.CreateAsync(dto, authorId, cancellationToken);
        return CreatedAtAction(nameof(GetBySlug), new { slug = recipe.Slug }, recipe);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RecipeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRecipeDto dto,
        CancellationToken cancellationToken)
    {
        var authorId = GetRequiredUserId();
        var recipe = await _recipeService.UpdateAsync(id, dto, authorId, cancellationToken);
        return Ok(recipe);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var authorId = GetRequiredUserId();
        await _recipeService.DeleteAsync(id, authorId, cancellationToken);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdClaim, out var parsedUserId)
            ? parsedUserId
            : null;
    }

    private Guid GetRequiredUserId()
    {
        var userId = GetCurrentUserId()
            ?? throw new UnauthorizedAuthException();
        return userId;
    }
}
