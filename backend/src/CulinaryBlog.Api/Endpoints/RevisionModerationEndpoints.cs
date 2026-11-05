using System.Security.Claims;
using System.Text.Json;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints;

public static class RevisionModerationEndpoints
{
    public static void MapRevisionModerationEndpoints(this WebApplication app)
    {
        var revisions = app.MapGroup("/api/v1/recipes/{id:guid}/revisions").RequireAuthorization().WithTags("Recipe Revisions");
        revisions.MapGet("/", GetRevisions);
        revisions.MapGet("/{version:int}", GetRevision);
        revisions.MapGet("/compare", CompareRevisions);
        revisions.MapPost("/{version:int}/rollback", Rollback);

        var moderation = app.MapGroup("/api/v1/admin/moderation")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Moderator"))
            .WithTags("Moderation");
        moderation.MapGet("/pending", GetPending);
        moderation.MapPost("/recipes/{id:guid}/approve", ApproveRecipe);
        moderation.MapPost("/recipes/{id:guid}/reject", RejectRecipe);
        moderation.MapPost("/comments/{id:guid}/approve", ApproveComment);
        moderation.MapPost("/comments/{id:guid}/reject", RejectComment);
        app.MapPost("/api/v1/recipes/{id:guid}/submit-for-approval", SubmitForApproval)
            .RequireAuthorization()
            .WithTags("Recipe Revisions")
            .WithSummary("Gửi công thức để kiểm duyệt");
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private static async Task<IResult> GetRevisions(Guid id, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct)
    {
        var allowed = await CanManage(id, principal, db, ct);
        if (!allowed) return Results.Forbid();
        var revisions = await db.RecipeRevisions.AsNoTracking().Where(x => x.RecipeId == id)
            .OrderByDescending(x => x.Version)
            .Select(x => new { x.Id, x.Version, x.Title, x.Description, x.ChangeSummary, x.EditorId, x.CreatedAt })
            .ToListAsync(ct);
        return Results.Ok(new { data = revisions });
    }

    private static async Task<IResult> GetRevision(Guid id, int version, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct)
    {
        if (!await CanManage(id, principal, db, ct)) return Results.Forbid();
        var revision = await db.RecipeRevisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RecipeId == id && x.Version == version, ct);
        return revision is null ? Results.NotFound() : Results.Ok(new { data = revision });
    }

    private static async Task<IResult> CompareRevisions(
        Guid id, int v1, int v2, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct)
    {
        if (!await CanManage(id, principal, db, ct)) return Results.Forbid();
        var revisions = await db.RecipeRevisions.AsNoTracking()
            .Where(x => x.RecipeId == id && (x.Version == v1 || x.Version == v2))
            .ToDictionaryAsync(x => x.Version, ct);
        if (!revisions.TryGetValue(v1, out var first) || !revisions.TryGetValue(v2, out var second))
            return Results.NotFound();
        return Results.Ok(new
        {
            data = new
            {
                from = new { first.Version, first.Title, first.Description, first.ContentJson },
                to = new { second.Version, second.Title, second.Description, second.ContentJson },
                recipeId = id,
                versionA = v1,
                versionB = v2,
                differences = CompareSnapshots(first.ContentJson, second.ContentJson)
            }
        });
    }

    private static async Task<IResult> Rollback(
        Guid id, int version, ClaimsPrincipal principal, ApplicationDbContext db,
        IRecipeCacheInvalidator invalidator, CancellationToken ct)
    {
        if (!await CanManage(id, principal, db, ct)) return Results.Forbid();
        var recipe = await db.Recipes.Include(x => x.Ingredients).Include(x => x.Steps).Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        var revision = await db.RecipeRevisions.SingleOrDefaultAsync(x => x.RecipeId == id && x.Version == version, ct);
        if (recipe is null || revision is null) return Results.NotFound();

        var snapshot = JsonSerializer.Deserialize<RecipeSnapshot>(revision.ContentJson);
        if (snapshot is null) return Results.Problem("Revision snapshot không hợp lệ.");
        var nextVersion = await db.RecipeRevisions.Where(x => x.RecipeId == id).Select(x => (int?)x.Version).MaxAsync(ct) ?? 0;
        var oldCategoryId = recipe.CategoryId;
        recipe.Title = snapshot.Title;
        recipe.CategoryId = snapshot.CategoryId;
        recipe.Description = snapshot.Description;
        recipe.Content = snapshot.Content;
        recipe.CoverImageUrl = snapshot.CoverImageUrl;
        recipe.PrepTimeMinutes = snapshot.PrepTimeMinutes;
        recipe.CookTimeMinutes = snapshot.CookTimeMinutes;
        recipe.Servings = snapshot.Servings;
        recipe.Difficulty = snapshot.Difficulty;
        recipe.Nutrition = snapshot.Nutrition;
        recipe.UpdatedAt = DateTime.UtcNow;
        db.RecipeIngredients.RemoveRange(recipe.Ingredients);
        db.RecipeSteps.RemoveRange(recipe.Steps);
        db.RecipeImages.RemoveRange(recipe.Images);
        recipe.Ingredients = snapshot.Ingredients.Select(x => new RecipeIngredient
        {
            RecipeId = id, Name = x.Name, Quantity = x.Quantity, Unit = x.Unit, Notes = x.Notes, SortOrder = x.SortOrder
        }).ToList();
        recipe.Steps = snapshot.Steps.Select(x => new RecipeStep
        {
            RecipeId = id, StepNumber = x.StepNumber, Title = x.Title, Description = x.Description, TimerMinutes = x.TimerMinutes
        }).ToList();
        recipe.Images = snapshot.Images.Select(x => new RecipeImage
        {
            RecipeId = id, OriginalUrl = x.OriginalUrl, MediumUrl = x.MediumUrl,
            ThumbnailUrl = x.ThumbnailUrl, AltText = x.AltText, IsPrimary = x.IsPrimary, SortOrder = x.SortOrder
        }).ToList();
        db.RecipeRevisions.Add(CreateRevision(recipe, GetUserId(principal), nextVersion + 1, "Khôi phục phiên bản " + version));
        await db.SaveChangesAsync(ct);
        await invalidator.InvalidateAsync(recipe.Id, recipe.Slug, oldCategoryId, null, ct);
        if (oldCategoryId != recipe.CategoryId)
            await invalidator.InvalidateAsync(recipe.Id, recipe.Slug, recipe.CategoryId, null, ct);
        return Results.Ok(new { data = new { recipe.Id, restoredFrom = version, newVersion = nextVersion + 1 } });
    }

    private static async Task<IResult> GetPending(ApplicationDbContext db, CancellationToken ct)
    {
        var recipes = await db.Recipes.AsNoTracking().Where(x => x.Status == RecipeStatus.PendingApproval)
            .Select(x => new { x.Id, x.Title, x.Description, x.AuthorId, x.CreatedAt }).ToListAsync(ct);
        var comments = await db.RecipeComments.AsNoTracking().Where(x => x.Status == ModerationStatus.Pending)
            .Select(x => new { x.Id, x.RecipeId, x.Content, x.UserId, x.CreatedAt }).ToListAsync(ct);
        return Results.Ok(new { data = new { recipes, comments } });
    }

    private static Task<IResult> ApproveRecipe(
        Guid id, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeCacheInvalidator invalidator, CancellationToken ct) =>
        SetModerationStatus(id, principal, db, invalidator, ModerationStatus.Approved, null, ct);

    private sealed record RejectRequest(string Reason);

    private static async Task<IResult> RejectRecipe(
        Guid id, RejectRequest request, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeCacheInvalidator invalidator, CancellationToken ct) =>
        await SetModerationStatus(id, principal, db, invalidator, ModerationStatus.Rejected, request.Reason, ct);

    private static async Task<IResult> SetModerationStatus(
        Guid id, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeCacheInvalidator invalidator,
        ModerationStatus action, string? reason, CancellationToken ct)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (recipe is null) return Results.NotFound();
        if (recipe.Status != RecipeStatus.PendingApproval)
            return Results.Conflict(new { title = "Công thức không ở trạng thái chờ duyệt." });
        if (action == ModerationStatus.Rejected && (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["reason"] = ["Lý do từ chối là bắt buộc và tối đa 1000 ký tự."] });
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        recipe.Status = action == ModerationStatus.Approved ? RecipeStatus.Published : RecipeStatus.Rejected;
        recipe.PublishedAt = action == ModerationStatus.Approved ? recipe.PublishedAt ?? DateTime.UtcNow : null;
        db.ModerationLogs.Add(new ModerationLog
        {
            TargetType = "Recipe", TargetId = id, ModeratorId = GetUserId(principal), Action = action, Reason = reason
        });
        await db.SaveChangesAsync(ct);
        await invalidator.InvalidateAsync(recipe.Id, recipe.Slug, recipe.CategoryId, null, ct);
        return Results.Ok(new { data = new { recipe.Id, recipe.Status, action, reason } });
    }

    private static Task<IResult> ApproveComment(
        Guid id, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct) =>
        SetCommentModerationStatus(id, principal, db, ModerationStatus.Approved, null, ct);

    private static Task<IResult> RejectComment(
        Guid id, RejectRequest request, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct) =>
        SetCommentModerationStatus(id, principal, db, ModerationStatus.Rejected, request.Reason, ct);

    private static async Task<IResult> SetCommentModerationStatus(
        Guid id, ClaimsPrincipal principal, ApplicationDbContext db, ModerationStatus action, string? reason, CancellationToken ct)
    {
        var comment = await db.RecipeComments.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (comment is null) return Results.NotFound();
        if (comment.Status != ModerationStatus.Pending)
            return Results.Conflict(new { title = "Bình luận không ở trạng thái chờ duyệt." });
        if (action == ModerationStatus.Rejected && (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["reason"] = ["Lý do từ chối là bắt buộc và tối đa 1000 ký tự."] });
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        comment.Status = action;
        comment.UpdatedAt = DateTime.UtcNow;
        db.ModerationLogs.Add(new ModerationLog
        {
            TargetType = "Comment", TargetId = id, ModeratorId = GetUserId(principal), Action = action, Reason = reason
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { data = new { comment.Id, comment.Status, action, reason } });
    }

    private static async Task<IResult> SubmitForApproval(
        Guid id, ClaimsPrincipal principal, ApplicationDbContext db, IRecipeCacheInvalidator invalidator, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .Include(x => x.Ingredients)
            .Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (recipe is null) return Results.NotFound();
        if (!principal.IsInRole("Admin") && !principal.IsInRole("Moderator") && recipe.AuthorId != GetUserId(principal))
            return Results.Forbid();
        if (recipe.Status is RecipeStatus.Published or RecipeStatus.Archived)
            return Results.Conflict(new { title = "Công thức đã công khai hoặc lưu trữ." });
        if (recipe.Ingredients.Count == 0 || recipe.Steps.Count == 0 || recipe.PrepTimeMinutes <= 0 || recipe.Servings <= 0)
            return Results.UnprocessableEntity(new { title = "Công thức cần có nguyên liệu, bước nấu và khẩu phần hợp lệ trước khi gửi duyệt." });
        recipe.Status = RecipeStatus.PendingApproval;
        recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await invalidator.InvalidateAsync(recipe.Id, recipe.Slug, recipe.CategoryId, null, ct);
        return Results.Accepted($"/api/v1/recipes/{id}/revisions", new { data = new { recipe.Id, status = recipe.Status } });
    }

    private static async Task<bool> CanManage(Guid recipeId, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct)
    {
        if (principal.IsInRole("Admin") || principal.IsInRole("Moderator")) return true;
        var userId = GetUserId(principal);
        return await db.Recipes.AnyAsync(x => x.Id == recipeId && x.AuthorId == userId, ct);
    }

    public static RecipeRevision CreateRevision(Recipe recipe, Guid editorId, int version, string summary) =>
        new()
        {
            RecipeId = recipe.Id, EditorId = editorId, Version = version, Title = recipe.Title,
            Description = recipe.Description, ChangeSummary = summary, ContentJson = JsonSerializer.Serialize(RecipeSnapshot.From(recipe))
        };

    private static IReadOnlyList<RevisionDifference> CompareSnapshots(string oldJson, string newJson)
    {
        using var oldDocument = JsonDocument.Parse(oldJson);
        using var newDocument = JsonDocument.Parse(newJson);
        var oldFields = Flatten(oldDocument.RootElement);
        var newFields = Flatten(newDocument.RootElement);
        return oldFields.Keys.Union(newFields.Keys).OrderBy(key => key, StringComparer.Ordinal)
            .Select(key =>
            {
                var hadOld = oldFields.TryGetValue(key, out var oldValue);
                var hasNew = newFields.TryGetValue(key, out var newValue);
                if (hadOld && hasNew && oldValue == newValue) return null;
                var changeType = !hadOld ? "Added" : !hasNew ? "Deleted" : "Modified";
                return new RevisionDifference(key, changeType, oldValue, newValue);
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();
    }

    private static Dictionary<string, string> Flatten(JsonElement element)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        Flatten(element, "$", result);
        return result;
    }

    private static void Flatten(JsonElement element, string path, IDictionary<string, string> result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
                Flatten(property.Value, $"{path}.{property.Name}", result);
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
                Flatten(item, $"{path}[{index++}]", result);
            if (index == 0) result[path] = "[]";
            return;
        }

        result[path] = element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : element.GetRawText();
    }

    private sealed record RecipeSnapshot(
        string Title,
        Guid CategoryId,
        string Description,
        string Content,
        string? CoverImageUrl,
        int PrepTimeMinutes,
        int CookTimeMinutes,
        int Servings,
        DifficultyLevel Difficulty,
        IReadOnlyList<IngredientSnapshot> Ingredients,
        IReadOnlyList<StepSnapshot> Steps,
        RecipeNutrition Nutrition,
        IReadOnlyList<ImageSnapshot> Images)
    {
        public static RecipeSnapshot From(Recipe recipe) => new(
            recipe.Title, recipe.CategoryId, recipe.Description, recipe.Content, recipe.CoverImageUrl, recipe.PrepTimeMinutes, recipe.CookTimeMinutes,
            recipe.Servings, recipe.Difficulty,
            recipe.Ingredients.Select(x => new IngredientSnapshot(x.Name, x.Quantity, x.Unit, x.Notes, x.SortOrder)).ToList(),
            recipe.Steps.Select(x => new StepSnapshot(x.StepNumber, x.Title, x.Description, x.TimerMinutes)).ToList(),
            recipe.Nutrition,
            recipe.Images.Select(x => new ImageSnapshot(x.OriginalUrl, x.MediumUrl, x.ThumbnailUrl, x.AltText, x.IsPrimary, x.SortOrder)).ToList());
    }

    private sealed record IngredientSnapshot(string Name, decimal? Quantity, string? Unit, string? Notes, int SortOrder);
    private sealed record StepSnapshot(int StepNumber, string Title, string Description, int? TimerMinutes);
    private sealed record ImageSnapshot(string OriginalUrl, string? MediumUrl, string? ThumbnailUrl, string? AltText, bool IsPrimary, int SortOrder);
    private sealed record RevisionDifference(string Field, string ChangeType, string? OldValue, string? NewValue);
}
