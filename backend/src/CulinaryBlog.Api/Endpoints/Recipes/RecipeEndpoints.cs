using CulinaryBlog.API.Extensions;
using System.Security.Claims;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeSteps;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using RecipeImageDto = CulinaryBlog.Application.Features.Recipes.Dtos.RecipeImageDto;
using RecipeStepDto = CulinaryBlog.Application.Features.Recipes.Dtos.RecipeStepDto;
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
using System.Text;
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
            .WithTags("Recipes")
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        // TV2: Danh sách công thức đã xuất bản
        group.MapGet("/", GetRecipes)
            .WithName("GetRecipes")
            .WithSummary("Lấy danh sách công thức đã xuất bản");

        app.MapGet("/api/v1/admin/recipes", GetAdminRecipes)
            .WithTags("AdminRecipes")
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("GetAdminRecipes")
            .WithSummary("Lấy danh sách công thức mọi trạng thái cho Admin")
            .Produces<PagedResultDto<RecipeListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.MapGet("/api/v1/admin/recipes/trash", GetDeletedRecipes)
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy)
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

        group.MapGet("/{slug}/servings", PreviewRecipeServings)
            .WithName("PreviewRecipeServings")
            .WithSummary("Xem trước nguyên liệu và dinh dưỡng theo số khẩu phần")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapPost("/{slug}/scale", ScaleRecipe)
            .WithName("ScaleRecipe")
            .WithSummary("Tính nguyên liệu và dinh dưỡng theo số khẩu phần")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGet("/{slug}/export/pdf", ExportRecipePdf)
            .WithName("ExportRecipePdf")
            .WithSummary("Xuất công thức thành file PDF có thể in");

        // TV2 + TV4: Chi tiết công thức với AsNoTracking, bảo vệ quyền, sắp xếp Step/Image và Double-Shield Cache
        group.MapGet("/{slug}", GetRecipeBySlug)
            .WithName("GetRecipeBySlug")
            .WithSummary("Lấy chi tiết công thức");

        // Recipe interactions: favorite, rating and comments
        group.MapPost("/{id:guid}/favorite", ToggleFavorite)
            .RequireAuthorization()
            .WithName("ToggleRecipeFavorite")
            .WithSummary("Thêm hoặc bỏ công thức khỏi danh sách yêu thích");

        group.MapGet("/{id:guid}/favorite", GetFavoriteStatus)
            .RequireAuthorization()
            .WithName("GetRecipeFavoriteStatus")
            .WithSummary("Kiểm tra trạng thái yêu thích của công thức");

        app.MapGet("/api/v1/me/favorites", GetMyFavorites)
            .RequireAuthorization()
            .WithName("GetMyFavoriteRecipes")
            .WithSummary("Lấy danh sách công thức đã lưu");

        group.MapGet("/{id:guid}/ratings", GetRecipeRatings)
            .WithName("GetRecipeRatings")
            .WithSummary("Lấy tổng quan đánh giá công thức");

        group.MapPut("/{id:guid}/ratings/me", UpsertMyRating)
            .RequireAuthorization()
            .WithName("UpsertMyRecipeRating")
            .WithSummary("Tạo hoặc cập nhật đánh giá của người dùng");

        group.MapDelete("/{id:guid}/ratings/me", DeleteMyRating)
            .RequireAuthorization()
            .WithName("DeleteMyRecipeRating")
            .WithSummary("Xóa đánh giá của người dùng");

        group.MapGet("/{id:guid}/comments", GetRecipeComments)
            .WithName("GetRecipeComments")
            .WithSummary("Lấy danh sách bình luận công thức");

        group.MapPost("/{id:guid}/comments", AddRecipeComment)
            .RequireAuthorization()
            .WithName("AddRecipeComment")
            .WithSummary("Thêm bình luận hoặc trả lời bình luận công thức");

        group.MapPut("/{id:guid}/comments/{commentId:guid}", UpdateRecipeComment)
            .RequireAuthorization()
            .WithName("UpdateRecipeComment")
            .WithSummary("Cập nhật bình luận công thức");

        group.MapDelete("/{id:guid}/comments/{commentId:guid}", DeleteRecipeComment)
            .RequireAuthorization()
            .WithName("DeleteRecipeComment")
            .WithSummary("Xóa bình luận công thức");

        // TV4: Thêm ảnh cho công thức (Multipart Form Upload / JSON URL)
        group.MapPost("/{recipeId:guid}/images", AddRecipeImage)
            .RequireRateLimiting(RateLimitingExtensions.UploadPolicy)
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

        // Sao chép công thức (Duplicate Recipe)
        group.MapPost("/{id:guid}/duplicate", DuplicateRecipe)
            .RequireAuthorization()
            .WithName("DuplicateRecipe")
            .WithSummary("Sao chép công thức (chỉ dành cho tác giả)")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // Phước: Cập nhật công thức
        group.MapPut("/{id:guid}", UpdateRecipe)
            .RequireAuthorization()
            .WithName("UpdateRecipe")
            .WithSummary("Cập nhật công thức");

        // Phước: Lấy ETag của công thức (cho Author / Admin) – phục vụ delete/lifecycle trên Draft/Archived
        group.MapGet("/{id:guid}/etag", GetRecipeEtagById)
            .RequireAuthorization()
            .WithName("GetRecipeEtagById")
            .WithSummary("Lấy phiên bản (ETag) của công thức theo ID");

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
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("RestoreRecipe")
            .WithSummary("Khôi phục công thức đã xóa mềm");

        app.MapDelete("/api/v1/admin/recipes/{id:guid}/purge", PurgeRecipe)
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("PurgeRecipe")
            .WithSummary("Xóa vĩnh viễn công thức khỏi hệ thống");

        // TV4: Thêm bước cho công thức (Server tự cấp StepNumber)
        group.MapPost("/{id:guid}/steps", AddRecipeStep)
            .WithName("AddRecipeStep")
            .WithSummary("Thêm bước thực hiện cho công thức");

        // Tải lên ảnh cho bước thực hiện (Multipart Form -> MinIO)
        group.MapPost("/{id:guid}/steps/images", UploadStepImage)
            .RequireRateLimiting(RateLimitingExtensions.UploadPolicy)
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
        var includeDraftAndArchived = string.Equals(
            status?.Trim(),
            "Vault",
            StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (includeDraftAndArchived)
            {
                if (!isMine)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["status"] = ["Status Vault chỉ được dùng cùng mine=true."]
                        },
                        statusCode: StatusCodes.Status422UnprocessableEntity,
                        type: "VALIDATION_ERROR");
                }
            }
            else
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
                            ["status"] = ["Status phải là Draft, Published, Archived hoặc Vault."]
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
            Status = parsedStatus,
            IncludeDraftAndArchived = includeDraftAndArchived
        };

        if (isMine || authorId.HasValue)
        {
            var privateResult = await sender.Send(
                new GetRecipesQuery(effectivePage, effectivePageSize, listOptions),
                cancellationToken);
            return Results.Ok(privateResult);
        }

        var generation = await cache.GetAsync<string>("recipes:cache:generation", cancellationToken) ?? "initial";
        var cacheKey = $"{generation}:{CreatePublicRecipeListCacheKey(
            effectivePage,
            effectivePageSize,
            listOptions)}";
        var cachedResult = await cache.GetAsync<PagedResultDto<RecipeListItemDto>>(
            cacheKey,
            cancellationToken);
        if (cachedResult is not null)
        {
            return Results.Ok(ApiResponse.FromPaged(cachedResult));
        }

        var result = await sender.Send(
            new GetRecipesQuery(effectivePage, effectivePageSize, listOptions),
            cancellationToken);
        await cache.SetAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(15),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> PreviewRecipeServings(
        string slug,
        int servings,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        if (servings is < 1 or > 1000)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["servings"] = ["Số khẩu phần phải nằm trong khoảng từ 1 đến 1000."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "VALIDATION_ERROR");
        }

        var recipe = await db.Recipes
            .AsNoTracking()
            .Where(item =>
                item.Slug == slug &&
                !item.IsDeleted &&
                item.Status == RecipeStatus.Published)
            .Select(item => new
            {
                item.Id,
                item.Servings,
                Ingredients = item.Ingredients
                    .Where(ingredient => !ingredient.IsDeleted)
                    .OrderBy(ingredient => ingredient.SortOrder)
                    .Select(ingredient => new
                    {
                        ingredient.Id,
                        ingredient.Name,
                        ingredient.Quantity,
                        ingredient.Unit,
                        ingredient.Notes,
                    })
                    .ToList(),
                Nutrition = item.Nutrition == null
                    ? null
                    : new
                    {
                        item.Nutrition.Calories,
                        item.Nutrition.Protein,
                        item.Nutrition.Carbohydrates,
                        item.Nutrition.Fat,
                        item.Nutrition.Fiber,
                        item.Nutrition.Sodium,
                    },
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (recipe is null)
        {
            return Results.NotFound(new { title = "Không tìm thấy công thức đã xuất bản" });
        }

        var originalServings = Math.Max(recipe.Servings, 1);
        var factor = (decimal)servings / originalServings;
        var ingredients = recipe.Ingredients.Select(ingredient => new
        {
            ingredient.Id,
            ingredient.Name,
            Quantity = ingredient.Quantity.HasValue
                ? ingredient.Quantity.Value * factor
                : (decimal?)null,
            ingredient.Unit,
            ingredient.Notes,
        });
        var nutrition = recipe.Nutrition is null
            ? null
            : new
            {
                Calories = recipe.Nutrition.Calories * factor,
                Protein = recipe.Nutrition.Protein * factor,
                Carbohydrates = recipe.Nutrition.Carbohydrates * factor,
                Fat = recipe.Nutrition.Fat * factor,
                Fiber = recipe.Nutrition.Fiber * factor,
                Sodium = recipe.Nutrition.Sodium * factor,
            };

        return Results.Ok(new
        {
            data = new
            {
                recipeId = recipe.Id,
                originalServings,
                targetServings = servings,
                ingredients,
                nutrition,
            },
        });
    }

    private sealed record ScaleRecipeRequest(int TargetServings);

    private static Task<IResult> ScaleRecipe(
        string slug,
        ScaleRecipeRequest request,
        ApplicationDbContext db,
        CancellationToken cancellationToken) =>
        PreviewRecipeServings(slug, request.TargetServings, db, cancellationToken);

    private static async Task<IResult> ExportRecipePdf(
        string slug,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes
            .AsNoTracking()
            .Include(x => x.Ingredients)
            .Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Slug == slug && !x.IsDeleted && x.Status == RecipeStatus.Published, cancellationToken);
        if (recipe is null)
            return Results.NotFound(new { title = "Không tìm thấy công thức đã xuất bản" });

        var lines = new List<string>
        {
            recipe.Title,
            $"Khau phan: {recipe.Servings}",
            string.Empty,
            "NGUYEN LIEU"
        };
        lines.AddRange(recipe.Ingredients.OrderBy(x => x.SortOrder).Select(x => $"- {x.Name}: {x.Quantity} {x.Unit}"));
        lines.Add(string.Empty);
        lines.Add("CAC BUOC");
        lines.AddRange(recipe.Steps.OrderBy(x => x.StepNumber).Select(x => $"{x.StepNumber}. {x.Title}: {x.Description}"));

        var pdf = BuildSimplePdf(lines);
        return Results.File(pdf, "application/pdf", $"{recipe.Slug}.pdf");
    }

    private static byte[] BuildSimplePdf(IEnumerable<string> lines)
    {
        var content = new StringBuilder("BT /F1 11 Tf 50 780 Td ");
        foreach (var line in lines)
        {
            var safe = line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            content.Append($"({safe}) Tj 0 -16 Td ");
        }
        content.Append("ET");
        var stream = Encoding.ASCII.GetBytes(content.ToString());
        using var output = new MemoryStream();
        using var writer = new StreamWriter(output, Encoding.ASCII, leaveOpen: true);
        writer.Write("%PDF-1.4\n");
        writer.Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        writer.Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        writer.Write("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");
        writer.Write("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        writer.Write($"5 0 obj\n<< /Length {stream.Length} >>\nstream\n");
        writer.Flush();
        output.Write(stream);
        writer.Write("\nendstream\nendobj\nxref\n0 6\n0000000000 65535 f \ntrailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n0\n%%EOF");
        writer.Flush();
        return output.ToArray();
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

    private static async Task<IResult> GetAdminRecipes(
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

        var effectivePage = page is > 0 ? page.Value : 1;
        var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, 100) : 12;
        var adminOptions = options with
        {
            CategorySlug = string.IsNullOrWhiteSpace(categorySlug) ? null : categorySlug.Trim(),
            CategoryId = categoryId,
            IncludeAllStatuses = true
        };
        var result = await sender.Send(
            new GetRecipesQuery(effectivePage, effectivePageSize, adminOptions),
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

        return Results.Ok(ApiResponse.Success(result));
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
            return Results.Ok(ApiResponse.FromPaged(cachedResult));
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

        return Results.Ok(ApiResponse.FromPaged(result));
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
        var generation = await cache.GetAsync<string>("recipes:cache:generation", ct) ?? "initial";
        var cacheKey = $"recipe:{generation}:{normalizedSlug}";

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
                await cache.SetAsync(cacheKey, recipe, TimeSpan.FromMinutes(30), ct);
            }

            if (!string.IsNullOrEmpty(recipe.Version))
            {
                httpContext.Response.Headers.ETag = $"\"{recipe.Version}\"";
            }

            return Results.Ok(ApiResponse.Success(recipe));
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
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Results.Json(
                new { title = "Unauthorized", status = 401, detail = "Authentication is required to upload images." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

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
                if (file == null)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]> { ["File"] = ["Vui lòng chọn tệp ảnh để tải lên (File is required)."] },
                        statusCode: StatusCodes.Status400BadRequest);
                }
                if (file.Length == 0)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]> { ["File"] = ["Tệp tải lên không được để trống (File is empty)."] },
                        statusCode: StatusCodes.Status422UnprocessableEntity);
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
                return Results.Created($"/api/v1/recipes/{recipeId}/images/{image.Id}", ApiResponse.Success(image));
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
                return Results.Created($"/api/v1/recipes/{recipeId}/images/{image.Id}", ApiResponse.Success(image));
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
                statusCode: StatusCodes.Status422UnprocessableEntity);
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
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Results.Json(
                new { title = "Unauthorized", status = 401, detail = "Authentication is required to update images." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

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
            return Results.Ok(ApiResponse.Success(updated));
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
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }

    private static async Task<IResult> SetPrimaryRecipeImage(
        Guid recipeId,
        Guid imageId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Results.Json(
                new { title = "Unauthorized", status = 401, detail = "Authentication is required to set primary image." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

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

            return Results.Ok(ApiResponse.Success(updated));
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
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Results.Json(
                new { title = "Unauthorized", status = 401, detail = "Authentication is required to delete images." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

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

            return Results.Created($"/api/v1/recipes/{id}/steps/{step.Id}", ApiResponse.Success(step));
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

            return Results.Ok(ApiResponse.Success(step));
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

            return Results.Ok(ApiResponse.Success(steps));
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
            return Results.Ok(ApiResponse.Success(new { url = result.Url }));
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
            if (string.Equals(trimmedSort, "newest", StringComparison.OrdinalIgnoreCase))
            {
                sortBy = "publishedAt";
                sortDirection = "desc";
            }
            else if (string.Equals(trimmedSort, "oldest", StringComparison.OrdinalIgnoreCase))
            {
                sortBy = "publishedAt";
                sortDirection = "asc";
            }
            else if (trimmedSort.StartsWith('-'))
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
                ? Results.Conflict(new { error = validation, code = "RECIPE_SLUG_CONFLICT" })
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
        db.RecipeRevisions.Add(CulinaryBlog.Api.Endpoints.RevisionModerationEndpoints.CreateRevision(
            recipe, authorId, 1, "Tạo công thức"));
        await db.SaveChangesAsync(cancellationToken);

        SetRecipeEtag(httpContext, recipe);
        return Results.Created($"/api/v1/recipes/{recipe.Slug}", ApiResponse.Success(new { recipe.Id, recipe.Slug, version = recipe.xmin.ToString() }));
    }

    private static async Task<IResult> DuplicateRecipe(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        ApplicationDbContext db,
        CulinaryBlog.Application.Interfaces.IRecipeRepository recipeRepository,
        IRecipeWriteService recipeWriteService,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(principal);
        if (currentUserId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var source = await db.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Images)
            .SingleOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (source is null)
        {
            return Results.NotFound(new { error = "Không tìm thấy công thức nguồn hoặc công thức đã bị xóa." });
        }

        if (source.AuthorId != currentUserId)
        {
            return Results.Forbid();
        }

        var copyTitle = $"{source.Title} (Bản sao)";
        if (copyTitle.Length > 200)
        {
            copyTitle = copyTitle[..200];
        }

        var baseSlug = $"{source.Slug}-copy";
        var uniqueSlug = await GenerateUniqueSlugAsync(baseSlug, recipeRepository, db, cancellationToken: cancellationToken);

        var duplicatedRecipe = new Recipe
        {
            AuthorId = currentUserId,
            CategoryId = source.CategoryId,
            Title = copyTitle,
            Slug = uniqueSlug,
            Description = source.Description,
            Content = source.Content,
            PrepTimeMinutes = source.PrepTimeMinutes,
            CookTimeMinutes = source.CookTimeMinutes,
            Servings = source.Servings,
            Difficulty = source.Difficulty,
            Status = RecipeStatus.Draft,
            CoverImageUrl = source.CoverImageUrl,
            Nutrition = source.Nutrition is null ? new RecipeNutrition() : new RecipeNutrition
            {
                Calories = source.Nutrition.Calories,
                Protein = source.Nutrition.Protein,
                Carbohydrates = source.Nutrition.Carbohydrates,
                Fat = source.Nutrition.Fat,
                Fiber = source.Nutrition.Fiber,
                Sodium = source.Nutrition.Sodium,
                Source = source.Nutrition.Source
            },
            Ingredients = source.Ingredients
                .OrderBy(i => i.SortOrder)
                .Select(i => new RecipeIngredient
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Notes = i.Notes,
                    SortOrder = i.SortOrder
                }).ToList(),
            Steps = source.Steps
                .OrderBy(s => s.StepNumber)
                .Select(s => new RecipeStep
                {
                    StepNumber = s.StepNumber,
                    Title = s.Title,
                    Description = s.Description,
                    TimerMinutes = s.TimerMinutes,
                    ImageUrl = s.ImageUrl
                }).ToList(),
            Images = source.Images
                .OrderBy(img => img.SortOrder)
                .Select(img => new RecipeImage
                {
                    OriginalUrl = img.OriginalUrl,
                    MediumUrl = img.MediumUrl,
                    ThumbnailUrl = img.ThumbnailUrl,
                    AltText = img.AltText,
                    IsPrimary = img.IsPrimary,
                    SortOrder = img.SortOrder
                }).ToList()
        };

        await recipeWriteService.CreateAsync(duplicatedRecipe, cancellationToken);
        db.RecipeRevisions.Add(CulinaryBlog.Api.Endpoints.RevisionModerationEndpoints.CreateRevision(
            duplicatedRecipe, currentUserId, 1, $"Sao chép từ công thức: {source.Title}"));
        await db.SaveChangesAsync(cancellationToken);

        SetRecipeEtag(httpContext, duplicatedRecipe);
        return Results.Created($"/api/v1/recipes/{duplicatedRecipe.Slug}", ApiResponse.Success(new
        {
            id = duplicatedRecipe.Id,
            slug = duplicatedRecipe.Slug,
            title = duplicatedRecipe.Title,
            status = duplicatedRecipe.Status.ToString(),
            version = duplicatedRecipe.xmin.ToString()
        }));
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

        if (recipe.Status == RecipeStatus.Published)
        {
            return Results.UnprocessableEntity(new { error = "Công thức đã xuất bản không được phép chỉnh sửa trực tiếp. Vui lòng hủy xuất bản về Kho lưu trữ trước khi chỉnh sửa." });
        }

        if (!MatchesIfMatchHeader(recipe, httpContext.Request.Headers.IfMatch.ToString()))
        {
            return Results.Conflict(new
            {
                error = "Recipe version is stale. Reload the recipe and retry.",
                code = "RECIPE_VERSION_CONFLICT"
            });
        }

        request = request with { AuthorId = recipe.AuthorId };
        var validation = await ValidateRequest(request, db, cancellationToken, id);
        if (validation is not null)
        {
            return validation == "Recipe slug already exists."
                ? Results.Conflict(new { error = validation, code = "RECIPE_SLUG_CONFLICT" })
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

        var previousSlug = recipe.Slug;
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
        if (recipe.Status == RecipeStatus.Draft &&
            !string.Equals(previousSlug, slug, StringComparison.Ordinal))
        {
            var historyExists = await db.RecipeSlugHistories.AnyAsync(
                history => history.RecipeId == recipe.Id && history.Slug == previousSlug,
                cancellationToken);
            if (!historyExists)
            {
                db.RecipeSlugHistories.Add(new RecipeSlugHistory
                {
                    RecipeId = recipe.Id,
                    Slug = previousSlug,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await recipeRepository.UpdateAsync(recipe, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await recipeWriteService.ReplaceContentsAsync(
            recipe,
            ToIngredients(request.Ingredients),
            ToSteps(request.Steps),
            ToImages(request.Images),
            cancellationToken);
        var nextRevision = await db.RecipeRevisions
            .Where(item => item.RecipeId == recipe.Id)
            .Select(item => (int?)item.Version)
            .MaxAsync(cancellationToken) ?? 0;
        db.RecipeRevisions.Add(CulinaryBlog.Api.Endpoints.RevisionModerationEndpoints.CreateRevision(
            recipe,
            GetUserId(principal),
            nextRevision + 1,
            "Cập nhật công thức"));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        recipe.xmin = await db.Recipes
            .AsNoTracking()
            .Where(item => item.Id == recipe.Id)
            .Select(item => item.xmin)
            .SingleAsync(cancellationToken);
        SetRecipeEtag(httpContext, recipe);
        return Results.Ok(ApiResponse.Success(new { recipe.Id, recipe.Slug, version = recipe.xmin.ToString() }));
    }

    /// <summary>
    /// Trả về ETag (xmin) của công thức theo ID, chỉ cho phép tác giả hoặc Admin truy cập.
    /// Dùng để lấy phiên bản mới nhất trước khi thực hiện Delete / status change trên Draft / Archived.
    /// </summary>
    private static async Task<IResult> GetRecipeEtagById(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        CulinaryBlog.Application.Interfaces.IRecipeRepository recipeRepository,
        CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(id, includeDetails: false, cancellationToken);
        if (recipe is null || recipe.IsDeleted)
        {
            return Results.NotFound(new { error = "Không tìm thấy công thức." });
        }

        if (!IsOwnerOrAdmin(principal, recipe.AuthorId))
        {
            return Results.Forbid();
        }

        SetRecipeEtag(httpContext, recipe);

        return Results.Ok(new
        {
            id = recipe.Id,
            slug = recipe.Slug,
            status = recipe.Status.ToString(),
            version = recipe.xmin.ToString(),
        });
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
            return Results.Conflict(new
            {
                error = "Recipe version is stale. Reload the recipe and retry.",
                code = "RECIPE_VERSION_CONFLICT"
            });
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
                return Results.Conflict(new
                {
                    error = "Recipe version is stale. Reload the recipe and retry.",
                    code = "RECIPE_VERSION_CONFLICT"
                });
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
                return Results.Ok(ApiResponse.Success(new { recipe.Id, status = recipe.Status.ToString(), version = recipe.xmin.ToString() }));
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
            return Results.Ok(ApiResponse.Success(new { recipe.Id, status = recipe.Status.ToString(), version = recipe.xmin.ToString() }));
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
            return Results.Conflict(new
            {
                error = "Recipe version is stale. Reload the recipe and retry.",
                code = "RECIPE_VERSION_CONFLICT"
            });
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
        return Results.Created($"/api/v1/recipes/{id}/ingredients/{ingredient.Id}", ApiResponse.Success(ingredient));
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
            return Results.Conflict(new
            {
                error = "Recipe version is stale. Reload the recipe and retry.",
                code = "RECIPE_VERSION_CONFLICT"
            });
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
        return Results.Ok(ApiResponse.Success(ingredient));
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
            return Results.Conflict(new
            {
                error = "Recipe version is stale. Reload the recipe and retry.",
                code = "RECIPE_VERSION_CONFLICT"
            });
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
        var recipe = await db.Recipes.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
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
        if (candidate == "*")
        {
            return true;
        }

        if (uint.TryParse(candidate, out var version) && version == recipe.xmin)
        {
            return true;
        }

        if (recipe.RowVersion != null && recipe.RowVersion.Length > 0 &&
            string.Equals(candidate, Convert.ToBase64String(recipe.RowVersion), StringComparison.Ordinal))
        {
            return true;
        }

        return false;
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

        var moderationText = string.Join(
            '\n',
            request.Title,
            request.Description ?? string.Empty,
            request.Content,
            string.Join('\n', (request.Ingredients ?? []).Select(item => item.Name)),
            string.Join('\n', (request.Steps ?? []).SelectMany(item => new[] { item.Title, item.Description })));
        var moderationViolation = CulinaryBlog.Api.Helpers.ModerationTextFilter.FindViolation(moderationText);
        if (moderationViolation is not null)
        {
            return $"Content moderation rejected the recipe ({moderationViolation}).";
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
                request.Nutrition.Fiber,
                request.Nutrition.Sodium
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
        nutrition.Sodium = request.Sodium;
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

    private static async Task<IResult> ToggleFavorite(
        Guid id,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        if (!await db.Recipes.AnyAsync(recipe => recipe.Id == id && !recipe.IsDeleted, cancellationToken))
        {
            return Results.NotFound(ApiResponse.Success<object>(new { message = "Không tìm thấy công thức." }));
        }

        var favorite = await db.RecipeFavorites
            .SingleOrDefaultAsync(item => item.RecipeId == id && item.UserId == userId, cancellationToken);

        if (favorite is null)
        {
            db.RecipeFavorites.Add(new RecipeFavorite
            {
                RecipeId = id,
                UserId = userId
            });
        }
        else
        {
            db.RecipeFavorites.Remove(favorite);
        }

        await db.SaveChangesAsync(cancellationToken);
        var isFavorite = favorite is null;
        return Results.Ok(ApiResponse.Success(new { recipeId = id, isFavorite }));
    }

    private static async Task<IResult> GetFavoriteStatus(
        Guid id,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var isFavorite = userId != Guid.Empty &&
            await db.RecipeFavorites.AnyAsync(
                item => item.RecipeId == id && item.UserId == userId,
                cancellationToken);
        return Results.Ok(ApiResponse.Success(new { recipeId = id, isFavorite }));
    }

    private static async Task<IResult> GetMyFavorites(
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        int page = 1,
        int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.RecipeFavorites
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.Recipe);

        var total = await query.CountAsync(cancellationToken);
        var recipes = await query
            .OrderByDescending(recipe => recipe.UpdatedAt ?? recipe.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(recipe => new
            {
                recipe.Id,
                recipe.Title,
                recipe.Slug,
                recipe.Description,
                recipe.PublishedAt,
                recipe.CoverImageUrl
            })
            .ToListAsync(cancellationToken);

        var meta = new PaginationMeta(
            page,
            pageSize,
            total,
            (int)Math.Ceiling(total / (double)pageSize),
            page * pageSize < total,
            page > 1);
        return Results.Ok(ApiResponse.Paged(recipes, meta));
    }

    private static async Task<IResult> GetRecipeRatings(
        Guid id,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var ratings = db.RecipeRatings.Where(item => item.RecipeId == id);
        var ratingCount = await ratings.CountAsync(cancellationToken);
        var averageRating = ratingCount == 0
            ? 0
            : await ratings.AverageAsync(item => (decimal)item.Score, cancellationToken);
        var userId = GetUserId(principal);
        var myScore = userId == Guid.Empty
            ? (int?)null
            : await ratings.Where(item => item.UserId == userId).Select(item => (int?)item.Score).SingleOrDefaultAsync(cancellationToken);

        return Results.Ok(ApiResponse.Success(new
        {
            recipeId = id,
            averageRating = Math.Round(averageRating, 1),
            ratingCount,
            myScore
        }));
    }

    private static async Task<IResult> UpsertMyRating(
        Guid id,
        RatingRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        if (request.Score is < 1 or > 5)
        {
            return Results.UnprocessableEntity(ApiResponse.Success<object>(new { message = "Điểm đánh giá phải từ 1 đến 5." }));
        }

        if (!await db.Recipes.AnyAsync(recipe => recipe.Id == id && !recipe.IsDeleted, cancellationToken))
        {
            return Results.NotFound(ApiResponse.Success<object>(new { message = "Không tìm thấy công thức." }));
        }

        var rating = await db.RecipeRatings.SingleOrDefaultAsync(
            item => item.RecipeId == id && item.UserId == userId,
            cancellationToken);

        if (rating is null)
        {
            db.RecipeRatings.Add(new RecipeRating { RecipeId = id, UserId = userId, Score = request.Score });
        }
        else
        {
            rating.Score = request.Score;
            rating.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetRecipeRatings(id, principal, db, cancellationToken);
    }

    private static async Task<IResult> DeleteMyRating(
        Guid id,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var rating = await db.RecipeRatings.SingleOrDefaultAsync(
            item => item.RecipeId == id && item.UserId == userId,
            cancellationToken);
        if (rating is null)
        {
            return Results.NotFound(ApiResponse.Success<object>(new { message = "Không tìm thấy đánh giá." }));
        }

        db.RecipeRatings.Remove(rating);
        await db.SaveChangesAsync(cancellationToken);
        return await GetRecipeRatings(id, principal, db, cancellationToken);
    }

    private static async Task<IResult> GetRecipeComments(
        Guid id,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var currentUserId = GetUserId(principal);
        var query = db.RecipeComments
            .AsNoTracking()
            .Where(comment => comment.RecipeId == id &&
                              comment.ParentCommentId == null &&
                              (comment.Status == CulinaryBlog.Domain.Enums.ModerationStatus.Approved ||
                               (currentUserId != Guid.Empty &&
                                comment.UserId == currentUserId &&
                                comment.Status == CulinaryBlog.Domain.Enums.ModerationStatus.Pending)));
        var total = await query.CountAsync(cancellationToken);
        var comments = await query
            .OrderByDescending(comment => comment.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(comment => new
            {
                comment.Id,
                comment.Content,
                comment.CreatedAt,
                comment.UpdatedAt,
                AuthorName = comment.User.DisplayName,
                UserId = comment.UserId,
                Status = comment.Status,
                Replies = comment.Replies
                    .Where(reply => !reply.IsDeleted &&
                                    (reply.Status == CulinaryBlog.Domain.Enums.ModerationStatus.Approved ||
                                     (currentUserId != Guid.Empty &&
                                      reply.UserId == currentUserId &&
                                      reply.Status == CulinaryBlog.Domain.Enums.ModerationStatus.Pending)))
                    .OrderBy(reply => reply.CreatedAt)
                    .Select(reply => new
                    {
                        reply.Id,
                        reply.Content,
                        reply.CreatedAt,
                        reply.UpdatedAt,
                        AuthorName = reply.User.DisplayName,
                        UserId = reply.UserId,
                        Status = reply.Status,
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var meta = new PaginationMeta(
            page,
            pageSize,
            total,
            (int)Math.Ceiling(total / (double)pageSize),
            page * pageSize < total,
            page > 1);
        return Results.Ok(ApiResponse.Paged(comments, meta));
    }

    private static async Task<IResult> AddRecipeComment(
        Guid id,
        CommentRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var content = request.Content?.Trim() ?? string.Empty;
        if (content.Length is < 1 or > 2000)
        {
            return Results.UnprocessableEntity(ApiResponse.Success<object>(new { message = "Nội dung bình luận phải từ 1 đến 2000 ký tự." }));
        }
        var moderationViolation = CulinaryBlog.Api.Helpers.ModerationTextFilter.FindViolation(content);
        if (moderationViolation is not null)
        {
            return Results.UnprocessableEntity(new { code = moderationViolation, message = "Nội dung không vượt qua kiểm tra kiểm duyệt." });
        }

        if (!await db.Recipes.AnyAsync(recipe => recipe.Id == id && !recipe.IsDeleted, cancellationToken))
        {
            return Results.NotFound(ApiResponse.Success<object>(new { message = "Không tìm thấy công thức." }));
        }

        if (request.ParentCommentId.HasValue &&
            !await db.RecipeComments.AnyAsync(
                comment => comment.Id == request.ParentCommentId.Value &&
                           comment.RecipeId == id &&
                           comment.ParentCommentId == null,
                cancellationToken))
        {
            return Results.NotFound(ApiResponse.Success<object>(new { message = "Không tìm thấy bình luận cha." }));
        }

        var comment = new RecipeComment
        {
            RecipeId = id,
            UserId = userId,
            ParentCommentId = request.ParentCommentId,
            Content = content
        };
        db.RecipeComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ApiResponse.Success(new { comment.Id, comment.Content, comment.Status, comment.CreatedAt }));
    }

    private static async Task<IResult> UpdateRecipeComment(
        Guid id,
        Guid commentId,
        CommentRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var content = request.Content?.Trim() ?? string.Empty;
        if (content.Length is < 1 or > 2000)
        {
            return Results.UnprocessableEntity(ApiResponse.Success<object>(new { message = "Nội dung bình luận phải từ 1 đến 2000 ký tự." }));
        }
        var moderationViolation = CulinaryBlog.Api.Helpers.ModerationTextFilter.FindViolation(content);
        if (moderationViolation is not null)
        {
            return Results.UnprocessableEntity(new { code = moderationViolation, message = "Nội dung không vượt qua kiểm tra kiểm duyệt." });
        }

        var comment = await db.RecipeComments.SingleOrDefaultAsync(
            item => item.Id == commentId && item.RecipeId == id,
            cancellationToken);
        if (comment is null)
        {
            return Results.NotFound(ApiResponse.Success<object>(new { message = "Không tìm thấy bình luận." }));
        }
        if (comment.UserId != GetUserId(principal) && !principal.IsInRole("Admin"))
        {
            return Results.Forbid();
        }

        comment.Content = content;
        comment.Status = CulinaryBlog.Domain.Enums.ModerationStatus.Pending;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ApiResponse.Success(new { comment.Id, comment.Content, comment.Status, comment.UpdatedAt }));
    }

    private static async Task<IResult> DeleteRecipeComment(
        Guid id,
        Guid commentId,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var comment = await db.RecipeComments.SingleOrDefaultAsync(
            item => item.Id == commentId && item.RecipeId == id,
            cancellationToken);
        if (comment is null)
        {
            return Results.NotFound(ApiResponse.Success<object>(new { message = "Không tìm thấy bình luận." }));
        }
        if (comment.UserId != GetUserId(principal) && !principal.IsInRole("Admin"))
        {
            return Results.Forbid();
        }

        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

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
public sealed record NutritionRequest(decimal Calories, decimal Protein, decimal Carbohydrates, decimal Fat, decimal Fiber, decimal Sodium);
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

public record RatingRequest(int Score);

public record CommentRequest(string? Content, Guid? ParentCommentId = null);
