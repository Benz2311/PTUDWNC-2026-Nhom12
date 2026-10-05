using System.Security.Claims;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using RecipeImageDto = CulinaryBlog.Application.Features.Recipes.Dtos.RecipeImageDto;
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

        // TV4: Thêm ảnh cho công thức (Multipart Form Upload / JSON URL)
        group.MapPost("/{recipeId:guid}/images", AddRecipeImage)
            .WithName("AddRecipeImage")
            .WithSummary("Tải lên ảnh cho công thức (Multipart Form hoặc JSON)")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<RecipeImageDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        // TV4: Cập nhật thông tin ảnh công thức (AltText, OrderIndex, IsPrimary)
        group.MapPatch("/{recipeId:guid}/images/{imageId:guid}", UpdateRecipeImage)
            .WithName("UpdateRecipeImage")
            .WithSummary("Cập nhật thông tin ảnh công thức")
            .Produces<RecipeImageDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // TV4: Đặt ảnh đại diện (Set Primary trong cùng transaction)
        group.MapPatch("/{recipeId:guid}/images/{imageId:guid}/primary", SetPrimaryRecipeImage)
            .WithName("SetPrimaryRecipeImage")
            .WithSummary("Đặt ảnh đại diện cho công thức");

        // TV4: Xóa mềm ảnh và tự động chuyển Primary cho ảnh OrderIndex nhỏ nhất
        group.MapDelete("/{recipeId:guid}/images/{imageId:guid}", DeleteRecipeImage)
            .WithName("DeleteRecipeImage")
            .WithSummary("Xóa mềm ảnh công thức")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
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
        ISender sender,
        ICacheService cache,
        CancellationToken ct)
    {
        var effectivePage = page > 0 ? page : 1;
        var effectivePageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var normalizedQ = q?.Trim().ToLowerInvariant() ?? string.Empty;

        // SRS FR-SRCH-001: Cache Redis 1 phút vary theo query/page/pageSize
        var cacheKey = $"search:{normalizedQ}:{effectivePage}:{effectivePageSize}";
        var cachedResult = await cache.GetAsync<PagedResult<RecipeSearchResultDto>>(cacheKey, ct);
        if (cachedResult != null)
        {
            return Results.Ok(cachedResult);
        }

        var query = new SearchRecipesQuery(q, effectivePage, effectivePageSize);
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
        HttpRequest httpRequest,
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
            if (httpRequest.HasFormContentType)
            {
                var form = await httpRequest.ReadFormAsync(ct);
                var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
                if (file == null || file.Length == 0)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]> { ["File"] = ["Vui lòng chọn tệp ảnh để tải lên (File is required)."] },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var altText = form["altText"].FirstOrDefault();
                var isPrimaryStr = form["isPrimary"].FirstOrDefault();
                bool? isPrimary = bool.TryParse(isPrimaryStr, out var p) ? p : null;

                await using var stream = file.OpenReadStream();
                var command = new UploadRecipeImageCommand(
                    recipeId,
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    altText,
                    isPrimary,
                    currentUserId,
                    isAdmin);

                var image = await sender.Send(command, ct);
                return Results.Created($"/api/v1/recipes/{recipeId}/images/{image.Id}", image);
            }
            else
            {
                var jsonRequest = await httpRequest.ReadFromJsonAsync<AddRecipeImageRequest>(ct);
                if (jsonRequest == null || string.IsNullOrWhiteSpace(jsonRequest.OriginalUrl))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]> { ["File"] = ["Yêu cầu tải lên form multipart hoặc JSON chứa OriginalUrl."] },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var command = new AddRecipeImageCommand(
                    recipeId,
                    jsonRequest.OriginalUrl,
                    jsonRequest.MediumUrl,
                    jsonRequest.ThumbnailUrl,
                    jsonRequest.AltText,
                    jsonRequest.IsPrimary,
                    jsonRequest.OrderIndex,
                    currentUserId,
                    isAdmin);

                var image = await sender.Send(command, ct);
                return Results.Created($"/api/v1/recipes/{recipeId}/images/{image.Id}", image);
            }
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 403);
        }
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.Count > 0 ? ex.Errors : new Dictionary<string, string[]> { ["error"] = [ex.Message] },
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Dịch vụ lưu trữ không khả dụng");
        }
    }

    private static async Task<IResult> UpdateRecipeImage(
        Guid recipeId,
        Guid imageId,
        [FromBody] UpdateRecipeImageRequest request,
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
            var command = new UpdateRecipeImageCommand(
                recipeId,
                imageId,
                request.AltText,
                request.OrderIndex,
                request.IsPrimary,
                currentUserId,
                isAdmin);

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
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.Count > 0 ? ex.Errors : new Dictionary<string, string[]> { ["error"] = [ex.Message] },
                statusCode: StatusCodes.Status400BadRequest);
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

public record UpdateRecipeImageRequest(
    string? AltText = null,
    int? OrderIndex = null,
    bool? IsPrimary = null
);
