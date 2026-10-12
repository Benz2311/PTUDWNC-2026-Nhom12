using System.Security.Claims;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeSteps;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using RecipeImageDto = CulinaryBlog.Application.Features.Recipes.Dtos.RecipeImageDto;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries.GetDeletedRecipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using CulinaryBlog.Application.Features.Search.Dtos;
using CulinaryBlog.Application.Features.Search.Queries.SearchRecipes;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CulinaryBlog.Api.Helpers;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;

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

        app.MapGet("/api/v1/admin/recipes/trash", GetDeletedRecipes)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("GetDeletedRecipes")
            .WithSummary("Lấy danh sách công thức đã xóa mềm")
            .Produces<PagedResultDto<RecipeTrashItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

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

        // Phước: Tạo công thức (Draft)
        group.MapPost("/", CreateRecipe)
            .RequireAuthorization()
            .WithName("CreateRecipe")
            .WithSummary("Tạo công thức mới (bản nháp)");

        // Phước: Cập nhật công thức
        group.MapPut("/{id:guid}", UpdateRecipe)
            .RequireAuthorization()
            .WithName("UpdateRecipe")
            .WithSummary("Cập nhật công thức");

        // Phước: Xóa mềm công thức
        group.MapDelete("/{id:guid}", DeleteRecipe)
            .RequireAuthorization()
            .WithName("DeleteRecipe")
            .WithSummary("Xóa mềm công thức");

        // Phước: Quản lý trạng thái công thức
        MapStatusEndpoint(group, "publish", RecipeStatus.Published);
        MapStatusEndpoint(group, "unpublish", RecipeStatus.Draft);
        MapStatusEndpoint(group, "archive", RecipeStatus.Archived);
        MapStatusEndpoint(group, "unarchive", RecipeStatus.Draft);

        // Phước: Quản lý nguyên liệu
        group.MapPost("/{id:guid}/ingredients", AddIngredient)
            .RequireAuthorization()
            .WithName("AddIngredient")
            .WithSummary("Thêm nguyên liệu");

        group.MapPut("/{recipeId:guid}/ingredients/{ingredientId:guid}", UpdateIngredient)
            .RequireAuthorization()
            .WithName("UpdateIngredient")
            .WithSummary("Cập nhật nguyên liệu");

        group.MapDelete("/{recipeId:guid}/ingredients/{ingredientId:guid}", DeleteIngredient)
            .RequireAuthorization()
            .WithName("DeleteIngredient")
            .WithSummary("Xóa nguyên liệu");

        // Phước: Admin khôi phục & xóa vĩnh viễn (purge)
        app.MapPost("/api/v1/admin/recipes/{id:guid}/restore", RestoreRecipe)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("RestoreRecipe")
            .WithSummary("Khôi phục công thức đã xóa mềm");

        app.MapDelete("/api/v1/admin/recipes/{id:guid}/purge", PurgeRecipe)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("PurgeRecipe")
            .WithSummary("Xóa vĩnh viễn công thức khỏi hệ thống");

        // TV4: Thêm bước cho công thức (Server tự cấp StepNumber)
        group.MapPost("/{id:guid}/steps", AddRecipeStep)
            .WithName("AddRecipeStep")
            .WithSummary("Thêm bước thực hiện cho công thức");

        // Tải lên ảnh cho bước thực hiện (Multipart Form -> MinIO)
        group.MapPost("/{id:guid}/steps/images", UploadStepImage)
            .WithName("UploadStepImage")
            .WithSummary("Tải lên ảnh cho bước thực hiện (Multipart Form)")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        // TV4: Cập nhật bước thực hiện
        group.MapPut("/{id:guid}/steps/{stepId:guid}", UpdateRecipeStep)
            .WithName("UpdateRecipeStep")
            .WithSummary("Cập nhật bước thực hiện");

        // TV4: Sắp xếp lại các bước thực hiện
        group.MapPut("/{id:guid}/steps/reorder", ReorderRecipeSteps)
            .WithName("ReorderRecipeSteps")
            .WithSummary("Sắp xếp lại các bước thực hiện");

        // TV4: Xóa mềm bước thực hiện và tự động đánh số lại
        group.MapDelete("/{id:guid}/steps/{stepId:guid}", DeleteRecipeStep)
            .WithName("DeleteRecipeStep")
            .WithSummary("Xóa mềm bước thực hiện và tự động đánh số lại");
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
        bool? mine,
        Guid? authorId,
        string? status,
        ClaimsPrincipal user,
        ISender sender,
        ICacheService cache,
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

        var isMine = mine == true;
        var isAdmin = user.IsInRole("Admin");
        if (isMine && authorId.HasValue)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["authorId"] = ["Không thể kết hợp mine=true với authorId."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "VALIDATION_ERROR");
        }

        Guid? currentUserId = null;
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        if (isMine && (!user.IsInRole("Author") || !currentUserId.HasValue))
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Chỉ tác giả (Author) mới có quyền xem công thức của chính mình.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "RECIPE_FORBIDDEN");
        }

        if (authorId.HasValue && !isAdmin)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Chỉ Admin mới có quyền lọc theo authorId.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "RECIPE_FORBIDDEN");
        }

        RecipeStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusName = Enum.GetNames<RecipeStatus>()
                .FirstOrDefault(name => string.Equals(
                    name,
                    status.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (statusName is null ||
                !Enum.TryParse<RecipeStatus>(statusName, true, out var matchedStatus))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["status"] = ["Status phải là Draft, Published hoặc Archived."]
                    },
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    type: "VALIDATION_ERROR");
            }

            parsedStatus = matchedStatus;
            if (!isMine && !authorId.HasValue)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["status"] = ["Status chỉ được dùng cùng mine=true hoặc authorId của Admin."]
                    },
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    type: "VALIDATION_ERROR");
            }
        }

        var effectivePage = page is > 0 ? page.Value : 1;
        var effectivePageSize = pageSize is > 0
            ? Math.Min(pageSize.Value, 100)
            : 12;
        var listOptions = options with
        {
            CategorySlug = string.IsNullOrWhiteSpace(categorySlug)
                ? null
                : categorySlug.Trim(),
            CategoryId = categoryId,
            AuthorId = isMine ? currentUserId : authorId,
            Status = parsedStatus
        };

        if (isMine || authorId.HasValue)
        {
            var privateResult = await sender.Send(
                new GetRecipesQuery(effectivePage, effectivePageSize, listOptions),
                cancellationToken);
            return Results.Ok(privateResult);
        }

        var cacheKey = CreatePublicRecipeListCacheKey(
            effectivePage,
            effectivePageSize,
            listOptions);
        var cachedResult = await cache.GetAsync<PagedResultDto<RecipeListItemDto>>(
            cacheKey,
            cancellationToken);
        if (cachedResult is not null)
        {
            return Results.Ok(cachedResult);
        }

        var result = await sender.Send(
            new GetRecipesQuery(effectivePage, effectivePageSize, listOptions),
            cancellationToken);
        await cache.SetAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(1),
            cancellationToken);

        return Results.Ok(result);
    }

    private static string CreatePublicRecipeListCacheKey(
        int page,
        int pageSize,
        RecipeListOptions options)
    {
        return string.Join(
            ':',
            "recipes:list",
            page,
            pageSize,
            Uri.EscapeDataString(options.Search ?? string.Empty),
            Uri.EscapeDataString(options.CategorySlug ?? string.Empty),
            options.CategoryId?.ToString("N") ?? string.Empty,
            options.Difficulty?.ToString() ?? string.Empty,
            options.MaxCookTimeMinutes?.ToString() ?? string.Empty,
            options.MaxTotalTimeMinutes?.ToString() ?? string.Empty,
            options.SortBy,
            options.SortDescending);
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

        if (result is null)
        {
            return Results.Problem(
                title: "Category not found",
                detail: $"Category with slug '{categorySlug}' was not found.",
                statusCode: StatusCodes.Status404NotFound,
                type: "CATEGORY_NOT_FOUND");
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetDeletedRecipes(
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetDeletedRecipesQuery(page ?? 1, pageSize ?? 12),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> SearchRecipes(
        [FromQuery] string? q,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? categorySlug,
        [FromQuery] string? difficulty,
        [FromQuery] int? maxCookTimeMinutes,
        [FromQuery] string? sortBy,
        ISender sender,
        ICacheService cache,
        CancellationToken ct)
    {
        var effectivePage = page is > 0 ? page.Value : 1;
        var effectivePageSize = pageSize is > 0 and <= 100 ? pageSize.Value : 10;
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
        HttpContext httpContext,
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
                if (!string.IsNullOrEmpty(cachedRecipe.Version))
                {
                    httpContext.Response.Headers.ETag = $"\"{cachedRecipe.Version}\"";
                }
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

            if (!string.IsNullOrEmpty(recipe.Version))
            {
                httpContext.Response.Headers.ETag = $"\"{recipe.Version}\"";
            }

            return Results.Ok(recipe);
        }
        catch (NotFoundException ex)
        {
            return Results.Problem(
                title: "Recipe not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound,
                type: "RECIPE_NOT_FOUND");
        }
        catch (ForbiddenException ex)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden,
                type: "RECIPE_FORBIDDEN");
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

    private static async Task<IResult> AddRecipeStep(
        Guid id,
        [FromBody] CreateRecipeStepRequest request,
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
            var command = new CreateRecipeStepCommand(
                id,
                request.Title,
                request.Description,
                request.TimerMinutes,
                request.ImageUrl,
                currentUserId,
                isAdmin);

            var step = await sender.Send(command, ct);

            return Results.Created($"/api/v1/recipes/{id}/steps/{step.Id}", step);
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

    private static async Task<IResult> UpdateRecipeStep(
        Guid id,
        Guid stepId,
        [FromBody] UpdateRecipeStepRequest request,
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
            var command = new UpdateRecipeStepCommand(
                id,
                stepId,
                request.Title,
                request.Description,
                request.TimerMinutes,
                request.ImageUrl,
                currentUserId,
                isAdmin);

            var step = await sender.Send(command, ct);

            return Results.Ok(step);
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

    private static async Task<IResult> ReorderRecipeSteps(
        Guid id,
        [FromBody] ReorderRecipeStepsRequest request,
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
            var command = new ReorderRecipeStepsCommand(
                id,
                request.StepIds,
                currentUserId,
                isAdmin);

            var steps = await sender.Send(command, ct);

            return Results.Ok(steps);
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

    private static async Task<IResult> UploadStepImage(
        Guid id,
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
            if (!httpRequest.HasFormContentType)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["File"] = ["Yêu cầu form multipart/form-data chứa trường 'file'."] },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
            if (file == null || file.Length == 0)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["File"] = ["Vui lòng chọn tệp ảnh để tải lên (File is required)."] },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            await using var stream = file.OpenReadStream();
            var command = new UploadRecipeStepImageCommand(
                id,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                currentUserId,
                isAdmin);

            var result = await sender.Send(command, ct);
            return Results.Ok(new { url = result.Url });
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

    private static async Task<IResult> DeleteRecipeStep(
        Guid id,
        Guid stepId,
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
            var command = new DeleteRecipeStepCommand(
                id,
                stepId,
                currentUserId,
                isAdmin);

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
        catch (ValidationException ex)
        {
            return Results.ValidationProblem(
                ex.Errors.Count > 0 ? ex.Errors : new Dictionary<string, string[]> { ["error"] = [ex.Message] },
                statusCode: StatusCodes.Status400BadRequest);
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
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "VALIDATION_ERROR");
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
                        ["difficulty"] = ["Difficulty phải là Easy, Medium, Hard hoặc Expert."]
                    },
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    type: "VALIDATION_ERROR");
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
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "VALIDATION_ERROR");
        }

        if (maxTotalTimeMinutes.HasValue && maxTotalTimeMinutes.Value <= 0)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["maxTotalTimeMinutes"] = ["Tổng thời gian phải lớn hơn 0."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "VALIDATION_ERROR");
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
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "VALIDATION_ERROR");
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
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "VALIDATION_ERROR");
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

    private static async Task<IResult> CreateRecipe(
        RecipeRequest request,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        ApplicationDbContext db,
        CulinaryBlog.Application.Interfaces.IRecipeRepository recipeRepository,
        IRecipeWriteService recipeWriteService,
        CancellationToken cancellationToken)
    {
        var authorId = GetUserId(principal);
        if (authorId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        request = request with { AuthorId = authorId };
        var validation = await ValidateRequest(request, db, cancellationToken);
        if (validation is not null)
        {
            return validation == "Recipe slug already exists."
                ? Results.Conflict(new { error = validation })
                : Results.UnprocessableEntity(new { error = validation });
        }

        var slug = await GenerateUniqueSlugAsync(request.Slug ?? request.Title, recipeRepository, db, cancellationToken: cancellationToken);

        var recipe = new Recipe
        {
            AuthorId = request.AuthorId!.Value,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Slug = slug,
            Description = request.Description?.Trim() ?? string.Empty,
            Content = request.Content.Trim(),
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            Servings = request.Servings,
            Difficulty = request.Difficulty,
            Status = RecipeStatus.Draft,
            Nutrition = ToNutrition(request.Nutrition),
            Images = ToImages(request.Images),
            Steps = ToSteps(request.Steps)
        };
        recipe.Ingredients = ToIngredients(request.Ingredients);
        await recipeWriteService.CreateAsync(recipe, cancellationToken);

        SetRecipeEtag(httpContext, recipe);
        return Results.Created($"/api/v1/recipes/{recipe.Slug}", new { recipe.Id, recipe.Slug, version = recipe.xmin.ToString() });
    }

    private static async Task<IResult> UpdateRecipe(
        Guid id,
        RecipeRequest request,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        ApplicationDbContext db,
        CulinaryBlog.Application.Interfaces.IRecipeRepository recipeRepository,
        IRecipeWriteService recipeWriteService,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetForUpdateAsync(id, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (!IsOwnerOrAdmin(principal, recipe.AuthorId))
        {
            return Results.Forbid();
        }

        if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
        {
            return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
        }

        request = request with { AuthorId = recipe.AuthorId };
        var validation = await ValidateRequest(request, db, cancellationToken, id);
        if (validation is not null)
        {
            return validation == "Recipe slug already exists."
                ? Results.Conflict(new { error = validation })
                : Results.UnprocessableEntity(new { error = validation });
        }

        var requestedSlug = request.Slug is null ? recipe.Slug : Slugify(request.Slug);
        if ((recipe.Status is RecipeStatus.Published or RecipeStatus.Archived) &&
            !string.Equals(requestedSlug, recipe.Slug, StringComparison.Ordinal))
        {
            return Results.UnprocessableEntity(new { error = "Published or archived recipes cannot change their slug." });
        }

        var slug = recipe.Status is RecipeStatus.Published or RecipeStatus.Archived
            ? recipe.Slug
            : await GenerateUniqueSlugAsync(request.Slug ?? request.Title, recipeRepository, db, id, cancellationToken);

        recipe.CategoryId = request.CategoryId;
        recipe.Title = request.Title.Trim();
        recipe.Slug = slug;
        recipe.Description = request.Description?.Trim() ?? string.Empty;
        recipe.Content = request.Content.Trim();
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.CookTimeMinutes = request.CookTimeMinutes;
        recipe.Servings = request.Servings;
        recipe.Difficulty = request.Difficulty;
        recipe.UpdatedAt = DateTime.UtcNow;
        recipe.Nutrition = ToNutrition(request.Nutrition, recipe.Nutrition);
        await recipeWriteService.ReplaceContentsAsync(
            recipe,
            ToIngredients(request.Ingredients),
            ToSteps(request.Steps),
            ToImages(request.Images),
            cancellationToken);

        SetRecipeEtag(httpContext, recipe);
        return Results.Ok(new { recipe.Id, recipe.Slug, version = recipe.xmin.ToString() });
    }

    private static async Task<IResult> DeleteRecipe(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        CulinaryBlog.Application.Interfaces.IRecipeRepository recipeRepository,
        IRecipeWriteService recipeWriteService,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(id, includeDetails: false, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (!IsOwnerOrAdmin(principal, recipe.AuthorId))
        {
            return Results.Forbid();
        }

        if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
        {
            return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
        }

        await recipeWriteService.DeleteAsync(recipe, cancellationToken);
        return Results.NoContent();
    }

    private static void MapStatusEndpoint(RouteGroupBuilder group, string action, RecipeStatus status)
    {
        async Task<IResult> UpdateStatus(
            Guid id,
            ClaimsPrincipal principal,
            HttpContext httpContext,
            ApplicationDbContext db,
            IRecipeWriteService recipeWriteService,
            IRecipeCacheInvalidator invalidator,
            CancellationToken cancellationToken)
        {
            var recipe = await db.Recipes
                .Include(item => item.Ingredients)
                .Include(item => item.Steps)
                .Include(item => item.Category)
                .SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
            if (recipe is null)
            {
                return Results.NotFound();
            }

            if (!await CanManageRecipeAsync(id, principal, db, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
            {
                return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
            }

            if (status == RecipeStatus.Published)
            {
                var author = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == recipe.AuthorId, cancellationToken);
                if (author is null || !author.EmailConfirmed)
                {
                    return Results.UnprocessableEntity(new { error = "The recipe author must have a confirmed email before publishing." });
                }

                if (!CanPublishRecipe(recipe, author))
                {
                    return Results.UnprocessableEntity(new { error = "Recipe must have valid fields, an active category, at least one ingredient, and at least one step before publishing." });
                }
            }

            if (recipe.Status == status)
            {
                SetRecipeEtag(httpContext, recipe);
                return Results.Ok(new { recipe.Id, status = recipe.Status.ToString(), version = recipe.xmin.ToString() });
            }

            if ((action == "unpublish" && recipe.Status != RecipeStatus.Published) ||
                (action == "unarchive" && recipe.Status != RecipeStatus.Archived))
            {
                return Results.Conflict(new { error = "Recipe is not in a state that supports this transition." });
            }

            recipe.Status = status;
            if (status == RecipeStatus.Published)
            {
                recipe.PublishedAt ??= DateTime.UtcNow;
            }
            recipe.UpdatedAt = DateTime.UtcNow;
            await recipeWriteService.UpdateAsync(recipe, cancellationToken);
            db.RecipeAuditLogs.Add(new RecipeAuditLog
            {
                RecipeId = recipe.Id,
                ActorId = GetUserId(principal),
                Action = action,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            var categorySlug = recipe.Category?.Slug;
            await invalidator.InvalidateAsync(recipe.Id, recipe.Slug, recipe.CategoryId, categorySlug, cancellationToken);
            SetRecipeEtag(httpContext, recipe);
            return Results.Ok(new { recipe.Id, status = recipe.Status.ToString(), version = recipe.xmin.ToString() });
        }

        group.MapPost("/{id:guid}/" + action, UpdateStatus).RequireAuthorization();
    }

    private static async Task<IResult> AddIngredient(
        Guid id,
        IngredientRequest request,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        ApplicationDbContext db,
        IRecipeWriteService recipeWriteService,
        CancellationToken cancellationToken)
    {
        var validation = ValidateIngredient(request);
        if (validation is not null)
        {
            return Results.UnprocessableEntity(new { error = validation });
        }

        var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (!await CanManageRecipeAsync(id, principal, db, cancellationToken))
        {
            return Results.Forbid();
        }

        if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
        {
            return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
        }

        var ingredient = new RecipeIngredient
        {
            RecipeId = id,
            Name = request.Name.Trim(),
            Quantity = ReadIngredientQuantity(request.Quantity),
            Unit = request.Unit?.Trim(),
            Notes = request.Notes?.Trim(),
            SortOrder = request.OrderIndex ?? request.SortOrder
        };
        await recipeWriteService.AddIngredientAsync(ingredient, cancellationToken);
        await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
        return Results.Created($"/api/v1/recipes/{id}/ingredients/{ingredient.Id}", ingredient);
    }

    private static async Task<IResult> UpdateIngredient(
        Guid recipeId,
        Guid ingredientId,
        IngredientRequest request,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        ApplicationDbContext db,
        IRecipeWriteService recipeWriteService,
        CancellationToken cancellationToken)
    {
        var validation = ValidateIngredient(request);
        if (validation is not null)
        {
            return Results.UnprocessableEntity(new { error = validation });
        }

        var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == recipeId && !item.IsDeleted, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
        {
            return Results.Forbid();
        }

        if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
        {
            return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
        }

        var ingredient = await db.RecipeIngredients.SingleOrDefaultAsync(item => item.Id == ingredientId && item.RecipeId == recipeId, cancellationToken);
        if (ingredient is null)
        {
            return Results.NotFound();
        }

        ingredient.Name = request.Name.Trim();
        ingredient.Quantity = ReadIngredientQuantity(request.Quantity);
        ingredient.Unit = request.Unit?.Trim();
        ingredient.Notes = request.Notes?.Trim();
        ingredient.SortOrder = request.OrderIndex ?? request.SortOrder;
        await recipeWriteService.UpdateIngredientAsync(ingredient, cancellationToken);
        await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
        return Results.Ok(ingredient);
    }

    private static async Task<IResult> DeleteIngredient(
        Guid recipeId,
        Guid ingredientId,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        ApplicationDbContext db,
        IRecipeWriteService recipeWriteService,
        CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == recipeId && !item.IsDeleted, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (!await CanManageRecipeAsync(recipeId, principal, db, cancellationToken))
        {
            return Results.Forbid();
        }

        if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
        {
            return Results.Conflict(new { error = "Recipe version is stale. Reload the recipe and retry." });
        }

        var ingredient = await db.RecipeIngredients.SingleOrDefaultAsync(item => item.Id == ingredientId && item.RecipeId == recipeId, cancellationToken);
        if (ingredient is null)
        {
            return Results.NotFound();
        }

        await recipeWriteService.DeleteIngredientAsync(ingredient, cancellationToken);
        await TouchRecipeAsync(recipe, db, httpContext, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RestoreRecipe(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        ApplicationDbContext db,
        IRecipeCacheInvalidator invalidator,
        CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        if (!recipe.IsDeleted)
        {
            return Results.UnprocessableEntity(new { error = "Recipe is not soft deleted." });
        }

        var deletedAt = recipe.DeletedAt ?? recipe.UpdatedAt ?? recipe.CreatedAt;
        if (DateTime.UtcNow - deletedAt > RecipePurgeService.RetentionPeriod)
        {
            return Results.Conflict(new { error = "Recipe retention expired and cannot be restored." });
        }

        recipe.IsDeleted = false;
        recipe.DeletedAt = null;
        recipe.UpdatedAt = DateTime.UtcNow;
        await db.RecipeAuditLogs.AddAsync(new RecipeAuditLog
        {
            RecipeId = recipe.Id,
            ActorId = GetUserId(principal),
            Action = "Restore",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        var categorySlug = await db.Categories
            .Where(category => category.Id == recipe.CategoryId)
            .Select(category => category.Slug)
            .SingleOrDefaultAsync(cancellationToken);
        await invalidator.InvalidateAsync(recipe.Id, recipe.Slug, recipe.CategoryId, categorySlug, cancellationToken);
        SetRecipeEtag(httpContext, recipe);
        return Results.Ok(new { recipe.Id, status = recipe.Status.ToString() });
    }

    private static async Task<IResult> PurgeRecipe(
        Guid id,
        ClaimsPrincipal principal,
        IRecipePurgeService purgeService,
        CancellationToken cancellationToken)
    {
        if (!await purgeService.PurgeAsync(id, GetUserId(principal), cancellationToken))
        {
            return Results.NotFound();
        }
        return Results.NoContent();
    }

    private static bool CanPublishRecipe(Recipe recipe, ApplicationUser author)
    {
        if (string.IsNullOrWhiteSpace(recipe.Title) || recipe.Title.Trim().Length is < 5 or > 200 ||
            string.IsNullOrWhiteSpace(recipe.Content) || recipe.PrepTimeMinutes <= 0 ||
            recipe.CookTimeMinutes < 0 || recipe.Servings <= 0 || !Enum.IsDefined(recipe.Difficulty) ||
            recipe.Description.Length > 2000 ||
            recipe.CategoryId == Guid.Empty ||
            recipe.Category is { IsDeleted: true } ||
            recipe.Ingredients.Count == 0 ||
            recipe.Ingredients.Any(ingredient => string.IsNullOrWhiteSpace(ingredient.Name) || ingredient.Name.Trim().Length > 200) ||
            recipe.Steps.Count == 0 ||
            recipe.Steps.Any(step => string.IsNullOrWhiteSpace(step.Description) || step.Description.Trim().Length > 2000 || step.Title.Trim().Length > 200))
        {
            return false;
        }

        if (!author.EmailConfirmed)
        {
            return false;
        }

        return true;
    }

    private static bool MatchesIfMatchHeader(Recipe recipe, string? ifMatchHeader)
    {
        if (string.IsNullOrWhiteSpace(ifMatchHeader))
        {
            return false;
        }

        var candidate = ifMatchHeader.Trim();
        if (candidate.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[2..];
        }

        candidate = candidate.Trim('"');
        return uint.TryParse(candidate, out var version) && version == recipe.xmin;
    }

    private static void SetRecipeEtag(HttpContext httpContext, Recipe recipe) =>
        httpContext.Response.Headers.ETag = $"\"{recipe.xmin}\"";

    private static async Task TouchRecipeAsync(Recipe recipe, ApplicationDbContext db, HttpContext httpContext, CancellationToken cancellationToken)
    {
        recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        SetRecipeEtag(httpContext, recipe);
    }

    private static string? ValidateIngredient(IngredientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
        {
            return "Ingredient name is required and must be at most 200 characters.";
        }

        if (!TryReadIngredientQuantity(request.Quantity, out var quantity))
        {
            return "Ingredient quantity must be a JSON number or null.";
        }

        if (quantity is <= 0)
        {
            return "Ingredient quantity must be greater than zero when provided.";
        }

        if (request.Unit?.Trim().Length > 50)
        {
            return "Ingredient unit must be at most 50 characters.";
        }

        if ((request.OrderIndex ?? request.SortOrder) < 0)
        {
            return "Ingredient sort order cannot be negative.";
        }

        if (request.Notes?.Length > 500)
        {
            return "Ingredient notes must be at most 500 characters.";
        }

        return null;
    }

    private static bool TryReadIngredientQuantity(JsonElement value, out decimal? quantity)
    {
        quantity = null;
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var parsed))
        {
            return false;
        }

        quantity = parsed;
        return true;
    }

    private static decimal? ReadIngredientQuantity(JsonElement value)
    {
        TryReadIngredientQuantity(value, out var quantity);
        return quantity;
    }

    private static string? ValidateStep(StepRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 2000)
        {
            return "Step description is required and must be at most 2000 characters.";
        }

        if (request.Title?.Trim().Length > 200)
        {
            return "Step title must be at most 200 characters.";
        }

        return null;
    }

    private static async Task<string?> ValidateRequest(RecipeRequest request, ApplicationDbContext db, CancellationToken cancellationToken, Guid? existingId = null)
    {
        if (!request.AuthorId.HasValue || request.AuthorId.Value == Guid.Empty || request.CategoryId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return "AuthorId, CategoryId, Title and Content are required.";
        }

        if (request.Title.Trim().Length < 5 || request.Title.Trim().Length > 200)
        {
            return "Title must be between 5 and 200 characters.";
        }

        if (request.Description?.Length > 2000)
        {
            return "Description must be at most 2000 characters.";
        }

        if (request.PrepTimeMinutes <= 0 || request.CookTimeMinutes < 0 || request.Servings <= 0)
        {
            return "PrepTimeMinutes must be greater than zero, CookTimeMinutes cannot be negative, and Servings must be greater than zero.";
        }

        if (!Enum.IsDefined(request.Difficulty))
        {
            return "Difficulty is invalid.";
        }

        if (request.Nutrition is not null && new[]
            {
                request.Nutrition.Calories,
                request.Nutrition.Protein,
                request.Nutrition.Carbohydrates,
                request.Nutrition.Fat,
                request.Nutrition.Fiber
            }.Any(value => value < 0))
        {
            return "Nutrition values cannot be negative.";
        }

        foreach (var ingredient in request.Ingredients ?? [])
        {
            var ingredientError = ValidateIngredient(ingredient);
            if (ingredientError is not null)
            {
                return ingredientError;
            }
        }

        if (!await db.Users.AnyAsync(user => user.Id == request.AuthorId!.Value, cancellationToken))
        {
            return "Author was not found.";
        }

        if (!await db.Categories.AnyAsync(category => category.Id == request.CategoryId && !category.IsDeleted, cancellationToken))
        {
            return "Category was not found.";
        }

        return null;
    }

    private static string Slugify(string value) => SlugHelper.Generate(value);

    private static async Task<string> GenerateUniqueSlugAsync(string value, CulinaryBlog.Application.Interfaces.IRecipeRepository repository, ApplicationDbContext db, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var baseSlug = Slugify(value);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "recipe";
        }

        var candidate = baseSlug[..Math.Min(baseSlug.Length, 220)];
        var suffix = 2;
        while (await repository.ExistsBySlugAsync(candidate, excludeId, cancellationToken) ||
               await db.RecipeSlugHistories.AnyAsync(history => history.Slug == candidate && history.RecipeId != excludeId, cancellationToken))
        {
            var suffixText = $"-{suffix++}";
            candidate = $"{baseSlug[..Math.Min(baseSlug.Length, 220 - suffixText.Length)]}{suffixText}";
        }

        return candidate;
    }

    private static RecipeNutrition ToNutrition(NutritionRequest? request, RecipeNutrition? existing = null)
    {
        var nutrition = existing ?? new RecipeNutrition();
        if (request is null)
        {
            return nutrition;
        }

        nutrition.Source = NutritionSource.Manual;
        nutrition.Calories = request.Calories;
        nutrition.Protein = request.Protein;
        nutrition.Carbohydrates = request.Carbohydrates;
        nutrition.Fat = request.Fat;
        nutrition.Fiber = request.Fiber;
        return nutrition;
    }

    private static List<RecipeIngredient> ToIngredients(IEnumerable<IngredientRequest>? requests) =>
        (requests ?? []).Select((request, index) => new RecipeIngredient
        {
            Name = request.Name.Trim(),
            Quantity = ReadIngredientQuantity(request.Quantity),
            Unit = request.Unit?.Trim(),
            Notes = request.Notes?.Trim(),
            SortOrder = (request.OrderIndex ?? request.SortOrder) == 0 ? index : request.OrderIndex ?? request.SortOrder
        }).ToList();

    private static List<RecipeImage> ToImages(IEnumerable<ImageRequest>? requests) =>
        (requests ?? [])
            .Where(request => !string.IsNullOrWhiteSpace(request.Url))
            .Select((request, index) => new RecipeImage
            {
                Url = request.Url.Trim(),
                AltText = request.AltText?.Trim(),
                SortOrder = index,
                IsPrimary = index == 0
            })
            .ToList();

    private static List<RecipeStep> ToSteps(IEnumerable<StepRequest>? requests) =>
        (requests ?? [])
            .Where(request => !string.IsNullOrWhiteSpace(request.Description))
            .Select((request, index) => new RecipeStep
            {
                StepNumber = index + 1,
                Title = request.Title?.Trim() ?? string.Empty,
                Description = request.Description.Trim(),
                TimerMinutes = request.TimerMinutes,
                ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()
            })
            .ToList();

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)
            ? userId
            : Guid.Empty;

    private static bool IsOwnerOrAdmin(ClaimsPrincipal principal, Guid authorId) =>
        (principal.IsInRole("Admin") && GetUserId(principal) != Guid.Empty) ||
        GetUserId(principal) == authorId;

    private static async Task<bool> CanManageRecipeAsync(Guid recipeId, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        if (principal.IsInRole("Admin"))
        {
            return true;
        }

        var authorId = await db.Recipes
            .Where(recipe => recipe.Id == recipeId && !recipe.IsDeleted)
            .Select(recipe => recipe.AuthorId)
            .SingleOrDefaultAsync(cancellationToken);
        return authorId != Guid.Empty && authorId == GetUserId(principal);
    }
}

public sealed record RecipeRequest(Guid? AuthorId, Guid CategoryId, string Title, string? Slug, string? Description, string Content, int PrepTimeMinutes, int CookTimeMinutes, int Servings, DifficultyLevel Difficulty, NutritionRequest? Nutrition, IEnumerable<IngredientRequest>? Ingredients, IEnumerable<ImageRequest>? Images, IEnumerable<StepRequest>? Steps = null);
public sealed record NutritionRequest(decimal Calories, decimal Protein, decimal Carbohydrates, decimal Fat, decimal Fiber);
public sealed record IngredientRequest(string Name, JsonElement Quantity, string? Unit, int SortOrder = 0, string? Notes = null, int? OrderIndex = null);
public sealed record ImageRequest(string Url, string? AltText);
public sealed record StepRequest(string? Title, string Description, int? TimerMinutes = null, string? ImageUrl = null);

public record AddRecipeImageRequest(
    string OriginalUrl,
    string? MediumUrl = null,
    string? ThumbnailUrl = null,
    string? AltText = null,
    bool? IsPrimary = null,
    int? OrderIndex = null
);

public record CreateRecipeStepRequest(
    string? Title,
    string Description,
    int? TimerMinutes = null,
    string? ImageUrl = null
);

public record UpdateRecipeStepRequest(
    string? Title,
    string Description,
    int? TimerMinutes = null,
    string? ImageUrl = null
);

public record ReorderRecipeStepsRequest(
    List<Guid> StepIds
);

public record UpdateRecipeImageRequest(
    string? AltText = null,
    int? OrderIndex = null,
    bool? IsPrimary = null
);
