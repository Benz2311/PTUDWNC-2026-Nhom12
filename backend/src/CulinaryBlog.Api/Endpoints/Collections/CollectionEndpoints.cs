using System.Security.Claims;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Collections;

public static class CollectionEndpoints
{
    public static void MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/collections")
            .WithTags("Recipe Collections");

        group.MapGet("/", GetUserCollections)
            .RequireAuthorization()
            .WithName("GetUserCollections")
            .WithSummary("Lấy danh sách các bộ sưu tập của người dùng hiện tại");

        group.MapGet("/{id:guid}", GetCollectionById)
            .WithName("GetCollectionById")
            .WithSummary("Xem chi tiết bộ sưu tập công thức");

        group.MapPost("/", CreateCollection)
            .RequireAuthorization()
            .WithName("CreateCollection")
            .WithSummary("Tạo bộ sưu tập mới");

        group.MapPut("/{id:guid}", UpdateCollection)
            .RequireAuthorization()
            .WithName("UpdateCollection")
            .WithSummary("Cập nhật thông tin bộ sưu tập");

        group.MapDelete("/{id:guid}", DeleteCollection)
            .RequireAuthorization()
            .WithName("DeleteCollection")
            .WithSummary("Xóa bộ sưu tập");

        group.MapPost("/{id:guid}/recipes", AddRecipeToCollection)
            .RequireAuthorization()
            .WithName("AddRecipeToCollection")
            .WithSummary("Thêm công thức vào bộ sưu tập");

        group.MapDelete("/{id:guid}/recipes/{recipeId:guid}", RemoveRecipeFromCollection)
            .RequireAuthorization()
            .WithName("RemoveRecipeFromCollection")
            .WithSummary("Xóa công thức khỏi bộ sưu tập");

        // Helper endpoint to check collections containing a recipe
        app.MapGet("/api/v1/recipes/{id:guid}/collections", GetRecipeCollectionsForUser)
            .RequireAuthorization()
            .WithTags("Recipe Collections")
            .WithName("GetRecipeCollectionsForUser")
            .WithSummary("Lấy danh sách ID các bộ sưu tập của người dùng có chứa công thức này");
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(idStr, out var id) ? id : null;
    }

    private static async Task<IResult> GetUserCollections(
        ClaimsPrincipal userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(userPrincipal);
        if (userId == null) return Results.Unauthorized();

        var collections = await db.RecipeCollections
            .AsNoTracking()
            .Where(c => c.UserId == userId.Value && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.IsPrivate,
                c.UserId,
                UserName = c.User.DisplayName ?? c.User.UserName,
                RecipeCount = c.Items.Count,
                c.CreatedAt,
                c.UpdatedAt,
                PreviewImages = c.Items
                    .Take(4)
                    .Select(i => i.Recipe.CoverImageUrl ?? i.Recipe.Images.OrderBy(img => img.SortOrder).Select(img => img.OriginalUrl).FirstOrDefault())
                    .Where(url => url != null)
                    .ToList()
            })
            .ToListAsync(ct);

        var result = collections.Select(c => new RecipeCollectionDto(
            c.Id,
            c.Name,
            c.Description,
            c.IsPrivate,
            c.UserId,
            c.UserName,
            c.RecipeCount,
            c.CreatedAt,
            c.UpdatedAt,
            c.PreviewImages!
        )).ToList();

        return Results.Ok(ApiResponse.Success(result));
    }

    private static async Task<IResult> GetCollectionById(
        Guid id,
        ClaimsPrincipal userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var currentUserId = GetUserId(userPrincipal);
        var isAdmin = userPrincipal.IsInRole("Admin");

        var collection = await db.RecipeCollections
            .AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.Items)
                .ThenInclude(i => i.Recipe)
                    .ThenInclude(r => r.Category)
            .Include(c => c.Items)
                .ThenInclude(i => i.Recipe)
                    .ThenInclude(r => r.Images)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        if (collection == null)
        {
            return Results.Problem(
                title: "Collection not found",
                detail: $"Bộ sưu tập với ID '{id}' không tồn tại.",
                statusCode: StatusCodes.Status404NotFound,
                type: "COLLECTION_NOT_FOUND");
        }

        if (collection.IsPrivate && collection.UserId != currentUserId && !isAdmin)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Bạn không có quyền truy cập bộ sưu tập riêng tư này.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "FORBIDDEN");
        }

        var recipes = collection.Items
            .Where(i => !i.Recipe.IsDeleted && (i.Recipe.Status == Domain.Enums.RecipeStatus.Published || i.Recipe.AuthorId == currentUserId || isAdmin))
            .OrderByDescending(i => i.AddedAt)
            .Select(i => new RecipeCollectionItemDto(
                i.RecipeId,
                i.Recipe.Title,
                i.Recipe.Slug,
                i.Recipe.CoverImageUrl ?? i.Recipe.Images.OrderBy(img => img.SortOrder).Select(img => img.OriginalUrl).FirstOrDefault(),
                i.Recipe.Category?.Name ?? "Chưa phân loại",
                i.Recipe.Category?.Slug ?? "",
                i.Recipe.PrepTimeMinutes,
                i.Recipe.CookTimeMinutes,
                i.Recipe.Difficulty.ToString(),
                i.AddedAt
            ))
            .ToList();

        var detail = new RecipeCollectionDetailDto(
            collection.Id,
            collection.Name,
            collection.Description,
            collection.IsPrivate,
            collection.UserId,
            collection.User?.DisplayName ?? collection.User?.UserName ?? "Ẩn danh",
            collection.CreatedAt,
            collection.UpdatedAt,
            recipes
        );

        return Results.Ok(ApiResponse.Success(detail));
    }

    private static async Task<IResult> CreateCollection(
        CreateRecipeCollectionRequest request,
        ClaimsPrincipal userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(userPrincipal);
        if (userId == null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Name", new[] { "Tên bộ sưu tập không được để trống." } }
            }, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var collection = new RecipeCollection
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsPrivate = request.IsPrivate,
            CreatedAt = DateTime.UtcNow
        };

        db.RecipeCollections.Add(collection);
        await db.SaveChangesAsync(ct);

        var dto = new RecipeCollectionDto(
            collection.Id,
            collection.Name,
            collection.Description,
            collection.IsPrivate,
            collection.UserId,
            userPrincipal.Identity?.Name ?? "",
            0,
            collection.CreatedAt,
            collection.UpdatedAt,
            Array.Empty<string>()
        );

        return Results.Created($"/api/v1/collections/{collection.Id}", ApiResponse.Success(dto));
    }

    private static async Task<IResult> UpdateCollection(
        Guid id,
        UpdateRecipeCollectionRequest request,
        ClaimsPrincipal userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(userPrincipal);
        if (userId == null) return Results.Unauthorized();
        var isAdmin = userPrincipal.IsInRole("Admin");

        var collection = await db.RecipeCollections
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        if (collection == null)
        {
            return Results.Problem(
                title: "Collection not found",
                detail: $"Bộ sưu tập với ID '{id}' không tồn tại.",
                statusCode: StatusCodes.Status404NotFound,
                type: "COLLECTION_NOT_FOUND");
        }

        if (collection.UserId != userId.Value && !isAdmin)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Bạn không có quyền chỉnh sửa bộ sưu tập này.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "FORBIDDEN");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Name", new[] { "Tên bộ sưu tập không được để trống." } }
            }, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        collection.Name = request.Name.Trim();
        collection.Description = request.Description?.Trim();
        collection.IsPrivate = request.IsPrivate;
        collection.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse.Success(new
        {
            collection.Id,
            collection.Name,
            collection.Description,
            collection.IsPrivate,
            collection.UpdatedAt
        }));
    }

    private static async Task<IResult> DeleteCollection(
        Guid id,
        ClaimsPrincipal userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(userPrincipal);
        if (userId == null) return Results.Unauthorized();
        var isAdmin = userPrincipal.IsInRole("Admin");

        var collection = await db.RecipeCollections
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        if (collection == null)
        {
            return Results.Problem(
                title: "Collection not found",
                detail: $"Bộ sưu tập với ID '{id}' không tồn tại.",
                statusCode: StatusCodes.Status404NotFound,
                type: "COLLECTION_NOT_FOUND");
        }

        if (collection.UserId != userId.Value && !isAdmin)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Bạn không có quyền xóa bộ sưu tập này.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "FORBIDDEN");
        }

        collection.IsDeleted = true;
        collection.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse.Success(new { message = "Bộ sưu tập đã được xóa thành công." }));
    }

    private static async Task<IResult> AddRecipeToCollection(
        Guid id,
        AddRecipeToCollectionRequest request,
        ClaimsPrincipal userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(userPrincipal);
        if (userId == null) return Results.Unauthorized();
        var isAdmin = userPrincipal.IsInRole("Admin");

        var collection = await db.RecipeCollections
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        if (collection == null)
        {
            return Results.Problem(
                title: "Collection not found",
                detail: $"Bộ sưu tập với ID '{id}' không tồn tại.",
                statusCode: StatusCodes.Status404NotFound,
                type: "COLLECTION_NOT_FOUND");
        }

        if (collection.UserId != userId.Value && !isAdmin)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Bạn không có quyền thêm công thức vào bộ sưu tập này.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "FORBIDDEN");
        }

        var recipeExists = await db.Recipes
            .AnyAsync(r => r.Id == request.RecipeId && !r.IsDeleted, ct);

        if (!recipeExists)
        {
            return Results.Problem(
                title: "Recipe not found",
                detail: $"Công thức với ID '{request.RecipeId}' không tồn tại.",
                statusCode: StatusCodes.Status404NotFound,
                type: "RECIPE_NOT_FOUND");
        }

        var alreadyInCollection = await db.RecipeCollectionItems
            .AnyAsync(i => i.CollectionId == id && i.RecipeId == request.RecipeId, ct);

        if (alreadyInCollection)
        {
            return Results.Problem(
                title: "Conflict",
                detail: "Công thức này đã tồn tại trong bộ sưu tập.",
                statusCode: StatusCodes.Status409Conflict,
                type: "RECIPE_ALREADY_IN_COLLECTION");
        }

        var item = new RecipeCollectionItem
        {
            CollectionId = id,
            RecipeId = request.RecipeId,
            AddedAt = DateTime.UtcNow
        };

        db.RecipeCollectionItems.Add(item);
        collection.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse.Success(new { message = "Đã thêm công thức vào bộ sưu tập thành công." }));
    }

    private static async Task<IResult> RemoveRecipeFromCollection(
        Guid id,
        Guid recipeId,
        ClaimsPrincipal userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(userPrincipal);
        if (userId == null) return Results.Unauthorized();
        var isAdmin = userPrincipal.IsInRole("Admin");

        var collection = await db.RecipeCollections
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        if (collection == null)
        {
            return Results.Problem(
                title: "Collection not found",
                detail: $"Bộ sưu tập với ID '{id}' không tồn tại.",
                statusCode: StatusCodes.Status404NotFound,
                type: "COLLECTION_NOT_FOUND");
        }

        if (collection.UserId != userId.Value && !isAdmin)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Bạn không có quyền chỉnh sửa bộ sưu tập này.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "FORBIDDEN");
        }

        var item = await db.RecipeCollectionItems
            .FirstOrDefaultAsync(i => i.CollectionId == id && i.RecipeId == recipeId, ct);

        if (item == null)
        {
            return Results.Problem(
                title: "Recipe not in collection",
                detail: "Công thức không nằm trong bộ sưu tập này.",
                statusCode: StatusCodes.Status404NotFound,
                type: "RECIPE_NOT_IN_COLLECTION");
        }

        db.RecipeCollectionItems.Remove(item);
        collection.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse.Success(new { message = "Đã xóa công thức khỏi bộ sưu tập." }));
    }

    private static async Task<IResult> GetRecipeCollectionsForUser(
        Guid id,
        ClaimsPrincipal userPrincipal,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(userPrincipal);
        if (userId == null) return Results.Unauthorized();

        var collections = await db.RecipeCollections
            .AsNoTracking()
            .Where(c => c.UserId == userId.Value && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.IsPrivate,
                ContainsRecipe = c.Items.Any(i => i.RecipeId == id)
            })
            .ToListAsync(ct);

        return Results.Ok(ApiResponse.Success(collections));
    }
}

