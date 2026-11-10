using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipePurgeService : IRecipePurgeService
{
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private readonly ApplicationDbContext _db;
    private readonly IStorageService? _storageService;
    private readonly IConfiguration _configuration;
    private readonly IRecipeCacheInvalidator _cacheInvalidator;

    public RecipePurgeService(
        ApplicationDbContext db,
        IConfiguration configuration,
        IRecipeCacheInvalidator cacheInvalidator,
        IStorageService? storageService = null)
    {
        _db = db;
        _storageService = storageService;
        _configuration = configuration;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<bool> PurgeAsync(Guid recipeId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var recipe = await _db.Recipes.IgnoreQueryFilters()
            .Include(item => item.Images)
            .Include(item => item.Ingredients)
            .Include(item => item.Steps)
            .SingleOrDefaultAsync(item => item.Id == recipeId, cancellationToken);
        if (recipe is null)
        {
            return false;
        }

        if (!recipe.IsDeleted)
        {
            if (actorId == Guid.Empty)
            {
                return false;
            }

            throw new CulinaryBlog.Domain.Exceptions.RecipeNotDeletedException();
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

        if (_storageService is not null)
        {
            var keys = recipe.Images
                .SelectMany(image => new[] { image.OriginalUrl, image.MediumUrl, image.ThumbnailUrl })
                .Select(url => GetManagedObjectName(url, recipeId))
                .OfType<string>()
                .Distinct(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                await _storageService.DeleteAsync(key, cancellationToken);
            }
        }

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

    private string? GetManagedObjectName(string? imageUrl, Guid recipeId)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri))
        {
            return null;
        }

        var bucket = _configuration["Storage:DefaultBucket"] ?? "culinaryblog";
        var configuredEndpoint = _configuration["Storage:ServiceUrl"];
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
        return path.StartsWith($"{bucketPrefix}recipes/{recipeId}/", StringComparison.OrdinalIgnoreCase)
            ? path[bucketPrefix.Length..]
            : null;
    }
}
