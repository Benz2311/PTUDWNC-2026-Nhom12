using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipePurgeService : IRecipePurgeService
{
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private readonly ApplicationDbContext _db;
    private readonly IMinioClient _minio;
    private readonly IConfiguration _configuration;
    private readonly IRecipeCacheInvalidator _cacheInvalidator;

    public RecipePurgeService(
        ApplicationDbContext db,
        IMinioClient minio,
        IConfiguration configuration,
        IRecipeCacheInvalidator cacheInvalidator)
    {
        _db = db;
        _minio = minio;
        _configuration = configuration;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<bool> PurgeAsync(Guid recipeId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var recipe = await _db.Recipes
            .Include(item => item.Images)
            .Include(item => item.Ingredients)
            .Include(item => item.Steps)
            .SingleOrDefaultAsync(item => item.Id == recipeId, cancellationToken);
        if (recipe is null)
        {
            return false;
        }

        foreach (var image in recipe.Images)
        {
            var objectName = GetManagedObjectName(image.Url);
            if (objectName is not null)
            {
                await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(_configuration["Minio:BucketName"] ?? "culinary-blog")
                    .WithObject(objectName), cancellationToken);
            }
        }

        var categorySlug = await _db.Categories
            .Where(category => category.Id == recipe.CategoryId)
            .Select(category => category.Slug)
            .SingleOrDefaultAsync(cancellationToken);
        var slug = recipe.Slug;

        _db.RecipeIngredients.RemoveRange(recipe.Ingredients);
        _db.RecipeSteps.RemoveRange(recipe.Steps);
        _db.RecipeImages.RemoveRange(recipe.Images);
        var slugHistory = await _db.RecipeSlugHistories
            .Where(item => item.RecipeId == recipeId)
            .ToListAsync(cancellationToken);
        _db.RecipeSlugHistories.RemoveRange(slugHistory);

        // Audit entries intentionally survive physical recipe deletion for traceability.
        _db.RecipeAuditLogs.Add(new RecipeAuditLog
        {
            RecipeId = recipeId,
            ActorId = actorId,
            Action = "Purge",
            CreatedAt = DateTime.UtcNow
        });
        _db.Recipes.Remove(recipe);
        await _db.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(recipeId, slug, recipe.CategoryId, categorySlug, cancellationToken);
        return true;
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - RetentionPeriod;
        var expiredIds = await _db.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.IsDeleted && (recipe.DeletedAt ?? recipe.UpdatedAt ?? recipe.CreatedAt) <= cutoff)
            .Select(recipe => recipe.Id)
            .ToListAsync(cancellationToken);

        var purged = 0;
        foreach (var recipeId in expiredIds)
        {
            if (await PurgeAsync(recipeId, Guid.Empty, cancellationToken))
            {
                purged++;
            }
        }

        return purged;
    }

    private string? GetManagedObjectName(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri))
        {
            return null;
        }

        var bucket = _configuration["Minio:BucketName"] ?? "culinary-blog";
        var configuredEndpoint = _configuration["Minio:PublicEndpoint"] ?? _configuration["Minio:Endpoint"];
        if (!Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var endpointUri) ||
            !string.Equals(imageUri.Authority, endpointUri.Authority, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = Uri.UnescapeDataString(imageUri.AbsolutePath).TrimStart('/');
        var endpointPath = Uri.UnescapeDataString(endpointUri.AbsolutePath).Trim('/');
        if (!string.IsNullOrEmpty(endpointPath))
        {
            var endpointPrefix = endpointPath + "/";
            if (!path.StartsWith(endpointPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            path = path[endpointPrefix.Length..];
        }

        var bucketPrefix = bucket + "/";
        return path.StartsWith(bucketPrefix, StringComparison.Ordinal)
            ? path[bucketPrefix.Length..]
            : null;
    }
}
