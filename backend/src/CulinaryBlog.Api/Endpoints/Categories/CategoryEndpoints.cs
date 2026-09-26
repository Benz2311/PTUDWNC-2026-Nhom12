using System.Text.Json;
using CulinaryBlog.Api.Helpers;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Categories;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        MapCategoryGroup(app.MapGroup("/api/categories").WithTags("Categories"));
        MapCategoryGroup(app.MapGroup("/api/v1/categories").WithTags("Categories"));
    }

    private static void MapCategoryGroup(RouteGroupBuilder group)
    {
        group.MapGet("", async (ApplicationDbContext db, IDistributedCache cache, CancellationToken cancellationToken) =>
        {
            var cached = await cache.GetStringAsync("categories:all", cancellationToken);
            if (cached is not null)
            {
                return Results.Content(cached, "application/json");
            }

            var categories = await db.Categories
                .AsNoTracking()
                .Where(category => !category.IsDeleted)
                .OrderBy(category => category.Name)
                .Select(category => new CategoryResponse(
                    category.Id,
                    category.Name,
                    category.Slug,
                    category.Description,
                    category.Recipes.Count(recipe => !recipe.IsDeleted)))
                .ToListAsync(cancellationToken);

            await cache.SetStringAsync(
                "categories:all",
                JsonSerializer.Serialize(categories),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) },
                cancellationToken);
            return Results.Ok(categories);
        });

        group.MapGet("/{slug}", async (string slug, ApplicationDbContext db, IDistributedCache cache, CancellationToken cancellationToken) =>
        {
            var cacheKey = $"categories:{slug}";
            var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Results.Content(cached, "application/json");
            }

            var category = await db.Categories
                .AsNoTracking()
                .Where(item => !item.IsDeleted && item.Slug == slug)
                .Select(item => new CategoryResponse(
                    item.Id,
                    item.Name,
                    item.Slug,
                    item.Description,
                    item.Recipes.Count(recipe => !recipe.IsDeleted)))
                .SingleOrDefaultAsync(cancellationToken);

            if (category is null)
            {
                return Results.NotFound();
            }

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(category),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) },
                cancellationToken);
            return Results.Ok(category);
        });

        group.MapPost("", async (CategoryRequest request, ApplicationDbContext db, IDistributedCache cache, CancellationToken cancellationToken) =>
        {
            var validation = Validate(request);
            if (validation is not null)
            {
                return Results.UnprocessableEntity(new { error = validation });
            }

            var slug = Slugify(request.Slug ?? request.Name);
            if (await db.Categories.AnyAsync(category => category.Slug == slug, cancellationToken))
            {
                return Results.Conflict(new { error = "Category slug already exists." });
            }

            var category = new Category
            {
                Name = request.Name.Trim(),
                Slug = slug,
                Description = request.Description?.Trim()
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync(cancellationToken);
            await InvalidateCacheAsync(cache, category.Slug, cancellationToken);

            return Results.Created($"/api/categories/{category.Slug}", ToResponse(category));
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPut("/{id:guid}", async (Guid id, CategoryRequest request, ApplicationDbContext db, IDistributedCache cache, CancellationToken cancellationToken) =>
        {
            var validation = Validate(request);
            if (validation is not null)
            {
                return Results.UnprocessableEntity(new { error = validation });
            }

            var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
            if (category is null)
            {
                return Results.NotFound();
            }

            var previousSlug = category.Slug;
            var slug = Slugify(request.Slug ?? request.Name);
            if (await db.Categories.AnyAsync(item => item.Id != id && item.Slug == slug, cancellationToken))
            {
                return Results.Conflict(new { error = "Category slug already exists." });
            }

            category.Name = request.Name.Trim();
            category.Slug = slug;
            category.Description = request.Description?.Trim();
            category.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await InvalidateCacheAsync(cache, category.Slug, cancellationToken, previousSlug);

            return Results.Ok(ToResponse(category));
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db, IDistributedCache cache, CancellationToken cancellationToken) =>
        {
            var category = await db.Categories
                .Include(item => item.Recipes)
                .SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
            if (category is null)
            {
                return Results.NotFound();
            }

            if (category.Recipes.Any(recipe => !recipe.IsDeleted))
            {
                return Results.Conflict(new { error = "Cannot delete a category that still has recipes." });
            }

            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await InvalidateCacheAsync(cache, category.Slug, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static CategoryResponse ToResponse(Category category) =>
        new(category.Id, category.Name, category.Slug, category.Description, 0);

    private static string? Validate(CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 150)
        {
            return "Name is required and must be at most 150 characters.";
        }

        return null;
    }

    private static string Slugify(string value) => SlugHelper.Generate(value);

    private static async Task InvalidateCacheAsync(IDistributedCache cache, string slug, CancellationToken cancellationToken, string? previousSlug = null)
    {
        await cache.RemoveAsync("categories:all", cancellationToken);
        await cache.RemoveAsync($"categories:{slug}", cancellationToken);
        if (!string.IsNullOrWhiteSpace(previousSlug) && previousSlug != slug)
        {
            await cache.RemoveAsync($"categories:{previousSlug}", cancellationToken);
        }
    }
}

public sealed record CategoryRequest(string Name, string? Slug, string? Description);
public sealed record CategoryResponse(Guid Id, string Name, string Slug, string? Description, int RecipeCount);
