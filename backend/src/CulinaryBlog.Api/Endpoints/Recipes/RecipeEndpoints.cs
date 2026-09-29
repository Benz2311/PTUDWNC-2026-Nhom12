using System.Security.Claims;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Search.Dtos;
using CulinaryBlog.Application.Features.Search.Queries.SearchRecipes;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.Api.Endpoints.Recipes;

public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipes");

        // 1. FR-SRCH-001: Tìm kiếm toàn văn công thức (FTS + Trigram fallback + Cache 1 phút)
        group.MapGet("/search", async (
            [FromQuery] string? q,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            ICacheService cache,
            CancellationToken ct) =>
        {
            int effectivePage = page is > 0 ? page.Value : 1;
            int effectivePageSize = pageSize is > 0 and <= 100 ? pageSize.Value : 10;
            var normalizedQ = q?.Trim().ToLowerInvariant() ?? string.Empty;

            // SRS FR-SRCH-001: Cache Redis 1 phút vary theo query/page/pageSize
            var cacheKey = $"search:{normalizedQ}:{effectivePage}:{effectivePageSize}";
            var cachedResult = await cache.GetAsync<CulinaryBlog.Application.Common.Models.PagedResult<RecipeSearchResultDto>>(cacheKey, ct);
            if (cachedResult != null)
            {
                return Results.Ok(cachedResult);
            }

            var query = new SearchRecipesQuery(q, effectivePage, effectivePageSize);
            var result = await sender.Send(query, ct);

            // Lưu cache 1 phút (tự động fail-safe nếu Redis offline)
            await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(1), ct);

            return Results.Ok(result);
        })
        .WithName("SearchRecipes");

        // 2. FR-RCP-002: Xem chi tiết công thức (AsNoTracking + Eager loading/Projection + Cache 5 phút)
        group.MapGet("/{slug}", async (
            string slug,
            ClaimsPrincipal user,
            ISender sender,
            ICacheService cache,
            CancellationToken ct) =>
        {
            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var cacheKey = $"recipe:{normalizedSlug}";

            // Trích xuất UserId và Role từ Claims an toàn (sẵn sàng khi TV1 gắn Auth middleware)
            Guid? currentUserId = null;
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                currentUserId = parsedUserId;
            }
            bool isAdmin = user.IsInRole("Admin");

            // Đọc shared cache
            var cachedRecipe = await cache.GetAsync<RecipeDetailDto>(cacheKey, ct);
            if (cachedRecipe != null)
            {
                // BẢO VỆ KÉP (Major-01): Nếu shared cache có chứa recipe, chỉ phục vụ trực tiếp nếu là Published!
                // Draft/Archived không bao giờ được phục vụ qua shared cache cho Guest
                if (cachedRecipe.Status == RecipeStatus.Published)
                {
                    return Results.Ok(cachedRecipe);
                }
            }

            try
            {
                var query = new GetRecipeBySlugQuery(slug, currentUserId, isAdmin);
                var recipe = await sender.Send(query, ct);

                // Major-01 Fix: CHỈ cache công thức có trạng thái Published!
                // Draft và Archived tuyệt đối KHÔNG được ghi vào shared cache recipe:{slug}
                if (recipe.Status == RecipeStatus.Published)
                {
                    await cache.SetAsync(cacheKey, recipe, TimeSpan.FromMinutes(5), ct);
                }

                return Results.Ok(recipe);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 403);
            }
        })
        .WithName("GetRecipeBySlug");

        // 3. FR-RCP-008: Thêm ảnh mới cho Recipe (Ảnh đầu tiên mặc định là primary)
        // TODO (TV1 Integration Point): Bổ sung .RequireAuthorization() khi TV1 hoàn tất Authentication middleware
        group.MapPost("/{recipeId:guid}/images", async (
            Guid recipeId,
            [FromBody] AddRecipeImageRequest request,
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
            bool isAdmin = user.IsInRole("Admin");

            try
            {
                var command = new AddRecipeImageCommand(
                    recipeId,
                    request.OriginalUrl,
                    request.MediumUrl,
                    request.ThumbnailUrl,
                    request.AltText,
                    request.IsPrimary,
                    request.OrderIndex,
                    currentUserId,
                    isAdmin);

                var image = await sender.Send(command, ct);

                return Results.Created($"/api/v1/recipes/{recipeId}/images/{image.Id}", image);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 403);
            }
        })
        .WithName("AddRecipeImage");

        // 4. FR-RCP-008: Đặt ảnh đại diện (Set Primary trong cùng transaction)
        // TODO (TV1 Integration Point): Bổ sung .RequireAuthorization() khi TV1 hoàn tất Authentication middleware
        group.MapPatch("/{recipeId:guid}/images/{imageId:guid}/primary", async (
            Guid recipeId,
            Guid imageId,
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
            bool isAdmin = user.IsInRole("Admin");

            try
            {
                var command = new SetPrimaryRecipeImageCommand(recipeId, imageId, currentUserId, isAdmin);
                var updated = await sender.Send(command, ct);

                return Results.Ok(updated);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 403);
            }
        })
        .WithName("SetPrimaryRecipeImage");

        // 5. FR-RCP-008: Xóa mềm ảnh và tự động gán primary cho ảnh OrderIndex nhỏ nhất còn lại
        // TODO (TV1 Integration Point): Bổ sung .RequireAuthorization() khi TV1 hoàn tất Authentication middleware
        group.MapDelete("/{recipeId:guid}/images/{imageId:guid}", async (
            Guid recipeId,
            Guid imageId,
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
            bool isAdmin = user.IsInRole("Admin");

            try
            {
                var command = new DeleteRecipeImageCommand(recipeId, imageId, currentUserId, isAdmin);
                await sender.Send(command, ct);

                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 403);
            }
        })
        .WithName("DeleteRecipeImage");

        return app;
    }
}

public record AddRecipeImageRequest(
    string OriginalUrl,
    string? MediumUrl = null,
    string? ThumbnailUrl = null,
    string? AltText = null,
    bool? IsPrimary = null,
    int? OrderIndex = null
);
