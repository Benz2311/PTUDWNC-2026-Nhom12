using System.Security.Claims;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Recipes;

public static class PersonalRecipeNoteEndpoints
{
    public static void MapPersonalRecipeNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/recipes/{recipeId:guid}/notes")
            .WithTags("Personal Recipe Notes")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        group.MapGet("/me", GetMyNote)
            .WithName("GetMyRecipeNote")
            .WithSummary("Lấy ghi chú cá nhân của người dùng cho công thức");

        group.MapGet("/", GetMyNote)
            .WithName("GetMyRecipeNotesDefault")
            .WithSummary("Lấy ghi chú cá nhân của người dùng cho công thức");

        group.MapPost("/", UpsertMyNote)
            .WithName("UpsertMyRecipeNote")
            .WithSummary("Tạo hoặc cập nhật ghi chú cá nhân cho công thức");

        group.MapPut("/{noteId:guid}", UpdateNote)
            .WithName("UpdatePersonalRecipeNote")
            .WithSummary("Cập nhật nội dung ghi chú cá nhân");

        group.MapDelete("/{noteId:guid}", DeleteNote)
            .WithName("DeletePersonalRecipeNote")
            .WithSummary("Xóa ghi chú cá nhân");

        group.MapDelete("/me", DeleteMyNote)
            .WithName("DeleteMyRecipeNote")
            .WithSummary("Xóa ghi chú cá nhân của người dùng cho công thức");
    }

    public sealed record PersonalRecipeNoteDto(
        Guid Id,
        Guid RecipeId,
        Guid UserId,
        string Content,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    public sealed record UpsertPersonalRecipeNoteRequest(string Content);

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private static async Task<IResult> GetMyNote(
        Guid recipeId,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var recipeExists = await db.Recipes
            .AnyAsync(r => r.Id == recipeId && !r.IsDeleted, cancellationToken);
        if (!recipeExists)
        {
            return Results.NotFound(new { error = "Không tìm thấy công thức." });
        }

        var note = await db.PersonalRecipeNotes
            .AsNoTracking()
            .SingleOrDefaultAsync(n => n.RecipeId == recipeId && n.UserId == userId, cancellationToken);

        if (note is null)
        {
            return Results.Ok(ApiResponse.Success<PersonalRecipeNoteDto?>(null));
        }

        var dto = new PersonalRecipeNoteDto(
            note.Id,
            note.RecipeId,
            note.UserId,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);

        return Results.Ok(ApiResponse.Success(dto));
    }

    private static async Task<IResult> UpsertMyNote(
        Guid recipeId,
        UpsertPersonalRecipeNoteRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request?.Content))
        {
            return Results.UnprocessableEntity(new { error = "Nội dung ghi chú không được để trống." });
        }

        var trimmedContent = request.Content.Trim();
        if (trimmedContent.Length > 5000)
        {
            return Results.UnprocessableEntity(new { error = "Nội dung ghi chú không được vượt quá 5000 ký tự." });
        }

        var recipeExists = await db.Recipes
            .AnyAsync(r => r.Id == recipeId && !r.IsDeleted, cancellationToken);
        if (!recipeExists)
        {
            return Results.NotFound(new { error = "Không tìm thấy công thức." });
        }

        var existingNote = await db.PersonalRecipeNotes
            .SingleOrDefaultAsync(n => n.RecipeId == recipeId && n.UserId == userId, cancellationToken);

        if (existingNote is not null)
        {
            existingNote.Content = trimmedContent;
            existingNote.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var updatedDto = new PersonalRecipeNoteDto(
                existingNote.Id,
                existingNote.RecipeId,
                existingNote.UserId,
                existingNote.Content,
                existingNote.CreatedAt,
                existingNote.UpdatedAt);

            return Results.Ok(ApiResponse.Success(updatedDto));
        }

        var newNote = new PersonalRecipeNote
        {
            Id = Guid.NewGuid(),
            RecipeId = recipeId,
            UserId = userId,
            Content = trimmedContent,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.PersonalRecipeNotes.Add(newNote);
        await db.SaveChangesAsync(cancellationToken);

        var createdDto = new PersonalRecipeNoteDto(
            newNote.Id,
            newNote.RecipeId,
            newNote.UserId,
            newNote.Content,
            newNote.CreatedAt,
            newNote.UpdatedAt);

        return Results.Created($"/api/v1/recipes/{recipeId}/notes/me", ApiResponse.Success(createdDto));
    }

    private static async Task<IResult> UpdateNote(
        Guid recipeId,
        Guid noteId,
        UpsertPersonalRecipeNoteRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request?.Content))
        {
            return Results.UnprocessableEntity(new { error = "Nội dung ghi chú không được để trống." });
        }

        var trimmedContent = request.Content.Trim();
        if (trimmedContent.Length > 5000)
        {
            return Results.UnprocessableEntity(new { error = "Nội dung ghi chú không được vượt quá 5000 ký tự." });
        }

        var note = await db.PersonalRecipeNotes
            .SingleOrDefaultAsync(n => n.Id == noteId && n.RecipeId == recipeId, cancellationToken);

        if (note is null)
        {
            return Results.NotFound(new { error = "Không tìm thấy ghi chú." });
        }

        if (note.UserId != userId)
        {
            return Results.Forbid();
        }

        note.Content = trimmedContent;
        note.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var dto = new PersonalRecipeNoteDto(
            note.Id,
            note.RecipeId,
            note.UserId,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);

        return Results.Ok(ApiResponse.Success(dto));
    }

    private static async Task<IResult> DeleteNote(
        Guid recipeId,
        Guid noteId,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var note = await db.PersonalRecipeNotes
            .SingleOrDefaultAsync(n => n.Id == noteId && n.RecipeId == recipeId, cancellationToken);

        if (note is null)
        {
            return Results.NotFound(new { error = "Không tìm thấy ghi chú." });
        }

        if (note.UserId != userId)
        {
            return Results.Forbid();
        }

        db.PersonalRecipeNotes.Remove(note);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteMyNote(
        Guid recipeId,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var note = await db.PersonalRecipeNotes
            .SingleOrDefaultAsync(n => n.RecipeId == recipeId && n.UserId == userId, cancellationToken);

        if (note is null)
        {
            return Results.NotFound(new { error = "Không tìm thấy ghi chú." });
        }

        db.PersonalRecipeNotes.Remove(note);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}

