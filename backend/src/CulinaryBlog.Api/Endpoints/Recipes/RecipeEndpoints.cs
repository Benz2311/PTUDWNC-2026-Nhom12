using System.Security.Claims;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using CulinaryBlog.Application.Features.Search.Dtos;
using CulinaryBlog.Application.Features.Search.Queries.SearchRecipes;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Endpoints;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/recipes")
            .WithTags("Recipes");

        // TV2: Danh sách công thức đã xuất bản
        group.MapGet("/", GetRecipes)
            .WithName("GetRecipes")
            .WithSummary("Lấy danh sách công thức đã xuất bản");

        // TV2: Danh sách công thức theo danh mục
        group.MapGet("/category/{categorySlug}", GetRecipesByCategory)
            .WithName("GetRecipesByCategory")
            .WithSummary("Lấy danh sách công thức theo danh mục");

        // TV4: Tìm kiếm toàn văn (FTS) & Trigram Fuzzy Fallback kèm Redis Cache
        group.MapGet("/search", SearchRecipes)
            .WithName("SearchRecipes")
            .WithSummary("Tìm kiếm công thức toàn văn và fuzzy");

        // TV2 + TV4: Chi tiết công thức với AsNoTracking, bảo vệ quyền, sắp xếp Step/Image và Double-Shield Cache
        group.MapGet("/{slug}", GetRecipeBySlug)
            .WithName("GetRecipeBySlug")
            .WithSummary("Lấy chi tiết công thức");

        // TV4: Thêm ảnh cho công thức (Ảnh đầu tiên mặc định Primary)
        group.MapPost("/{recipeId:guid}/images", AddRecipeImage)
            .WithName("AddRecipeImage")
            .WithSummary("Thêm ảnh cho công thức");

        // TV4: Đặt ảnh đại diện (Set Primary trong cùng transaction)
        group.MapPatch("/{recipeId:guid}/images/{imageId:guid}/primary", SetPrimaryRecipeImage)
            .WithName("SetPrimaryRecipeImage")
            .WithSummary("Đặt ảnh đại diện cho công thức");

        // TV4: Xóa mềm ảnh và tự động chuyển Primary cho ảnh OrderIndex nhỏ nhất
        group.MapDelete("/{recipeId:guid}/images/{imageId:guid}", DeleteRecipeImage)
            .WithName("DeleteRecipeImage")
            .WithSummary("Xóa mềm ảnh công thức");
    }

    private static async Task<IResult> GetRecipes(
        int? page,
        int? pageSize,
        string? search,
        string? categorySlug,
        Guid? categoryId,
        string? difficulty,
        int? maxCookTimeMinutes,
        int? maxTotalTimeMinutes,
        string? sortBy,
        string? sortDirection,
        string? sort,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateListOptions(
            search,
            difficulty,
            maxCookTimeMinutes,
            maxTotalTimeMinutes,
            sortBy,
            sortDirection,
            sort,
            out var options);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var result = await sender.Send(
            new GetRecipesQuery(page ?? 1, pageSize ?? 12, options with
            {
                CategorySlug = string.IsNullOrWhiteSpace(categorySlug)
                    ? null
                    : categorySlug.Trim(),
                CategoryId = categoryId
            }),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecipesByCategory(
        string categorySlug,
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetRecipesByCategoryQuery(categorySlug, page ?? 1, pageSize ?? 12),
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> SearchRecipes(
        [FromQuery] string? q,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? categorySlug,
        [FromQuery] string? difficulty,
        [FromQuery] int? maxCookTimeMinutes,
        [FromQuery] string? sortBy,
        ISender sender,
        ICacheService cache,
        CancellationToken ct)
    {
        var effectivePage = page > 0 ? page : 1;
        var effectivePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var rawQuery = q?.Trim() ?? string.Empty;
        var normalizedQ = rawQuery.ToLowerInvariant();

        DifficultyLevel? parsedDifficulty = null;
        if (!string.IsNullOrWhiteSpace(difficulty) && Enum.TryParse<DifficultyLevel>(difficulty, true, out var diff))
        {
            parsedDifficulty = diff;
        }

        // SRS FR-SRCH-001: Cache Redis 1 phút vary theo query, filter, page, pageSize, sort để tránh cache collision
        var catKey = categoryId?.ToString() ?? (!string.IsNullOrWhiteSpace(categorySlug) ? categorySlug.Trim().ToLowerInvariant() : "all");
        var diffKey = parsedDifficulty?.ToString() ?? "all";
        var cookKey = maxCookTimeMinutes?.ToString() ?? "any";
        var sortKey = string.IsNullOrWhiteSpace(sortBy) ? "relevance" : sortBy.Trim().ToLowerInvariant();

        var cacheKey = $"search:{normalizedQ}:cat={catKey}:diff={diffKey}:cook={cookKey}:sort={sortKey}:p={effectivePage}:sz={effectivePageSize}";
        var cachedResult = await cache.GetAsync<PagedResult<RecipeSearchResultDto>>(cacheKey, ct);
        if (cachedResult != null)
        {
            return Results.Ok(cachedResult);
        }

        var query = new SearchRecipesQuery(
            rawQuery,
            effectivePage,
            effectivePageSize,
            categoryId,
            categorySlug,
            parsedDifficulty,
            maxCookTimeMinutes,
            sortKey);

        var result = await sender.Send(query, ct);

        // Lưu cache 1 phút (tự động fail-safe nếu Redis offline)
        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(1), ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecipeBySlug(
        string slug,
        ClaimsPrincipal user,
        ISender sender,
        ICacheService cache,
        CancellationToken ct)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var cacheKey = $"recipe:{normalizedSlug}";

        Guid? currentUserId = null;
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }
        bool isAdmin = user.IsInRole("Admin");

        // Đọc shared cache
        var cachedRecipe = await cache.GetAsync<CulinaryBlog.Application.Features.Recipes.Dtos.RecipeDetailDto>(cacheKey, ct);
        if (cachedRecipe != null)
        {
            // BẢO VỆ KÉP (Major-01): Nếu shared cache có chứa recipe, chỉ phục vụ nếu là Published
            if (cachedRecipe.Status == RecipeStatus.Published)
            {
                return Results.Ok(cachedRecipe);
            }
        }

        try
        {
            var query = new GetRecipeBySlugQuery(slug, currentUserId, isAdmin);
            var recipe = await sender.Send(query, ct);

            // Major-01 Fix: CHỈ cache công thức có trạng thái Published
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
    }

    private static async Task<IResult> AddRecipeImage(
        Guid recipeId,
        [FromBody] AddRecipeImageRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
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
    }

    private static async Task<IResult> SetPrimaryRecipeImage(
        Guid recipeId,
        Guid imageId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
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
    }

    private static async Task<IResult> DeleteRecipeImage(
        Guid recipeId,
        Guid imageId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
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
    }

    private static IResult? ValidateListOptions(
        string? search,
        string? difficulty,
        int? maxCookTimeMinutes,
        int? maxTotalTimeMinutes,
        string? sortBy,
        string? sortDirection,
        string? sort,
        out RecipeListOptions options)
    {
        options = new RecipeListOptions();

        if (search?.Length > 200)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["search"] = ["Từ khóa tìm kiếm không được vượt quá 200 ký tự."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        DifficultyLevel? parsedDifficulty = null;
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            var difficultyName = Enum.GetNames<DifficultyLevel>()
                .FirstOrDefault(name => string.Equals(
                    name,
                    difficulty.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (difficultyName is null ||
                !Enum.TryParse<DifficultyLevel>(difficultyName, true, out var matchedDifficulty))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["difficulty"] = ["Difficulty phải là Easy, Medium hoặc Hard."]
                    },
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            parsedDifficulty = matchedDifficulty;
        }

        if (maxCookTimeMinutes.HasValue && maxCookTimeMinutes.Value <= 0)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["maxCookTimeMinutes"] = ["Thời gian nấu phải lớn hơn 0."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        if (maxTotalTimeMinutes.HasValue && maxTotalTimeMinutes.Value <= 0)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["maxTotalTimeMinutes"] = ["Tổng thời gian phải lớn hơn 0."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var trimmedSort = sort.Trim();
            if (trimmedSort.StartsWith('-'))
            {
                sortDirection = "desc";
                sortBy = trimmedSort[1..];
            }
            else
            {
                sortDirection = "asc";
                sortBy = trimmedSort;
            }
        }

        var sortByValue = string.IsNullOrWhiteSpace(sortBy)
            ? "publishedat"
            : sortBy.Trim().ToLowerInvariant();

        if (sortByValue is "createdat" or "-createdat")
        {
            sortByValue = "publishedat";
        }

        var parsedSortBy = sortByValue switch
        {
            "publishedat" => RecipeSortField.PublishedAt,
            "title" => RecipeSortField.Title,
            "preptime" => RecipeSortField.PrepTime,
            "cooktime" => RecipeSortField.CookTime,
            "totaltime" => RecipeSortField.TotalTime,
            _ => (RecipeSortField?)null
        };

        if (!parsedSortBy.HasValue)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["sortBy"] = ["SortBy phải là publishedAt, createdAt, title, prepTime, cookTime hoặc totalTime."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var sortDirectionValue = string.IsNullOrWhiteSpace(sortDirection)
            ? "desc"
            : sortDirection.Trim().ToLowerInvariant();
        var parsedSortDirection = sortDirectionValue switch
        {
            "asc" => false,
            "desc" => true,
            _ => (bool?)null
        };

        if (!parsedSortDirection.HasValue)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["sortDirection"] = ["SortDirection phải là asc hoặc desc."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        options = new RecipeListOptions(
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Difficulty: parsedDifficulty,
            MaxTotalTimeMinutes: maxTotalTimeMinutes,
            SortBy: parsedSortBy.Value,
            SortDescending: parsedSortDirection.Value,
            MaxCookTimeMinutes: maxCookTimeMinutes);
        return null;
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