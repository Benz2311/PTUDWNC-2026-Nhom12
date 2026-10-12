using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class RecipeCacheInvalidator : IRecipeCacheInvalidator
{
    private readonly IDistributedCache _cache;

    public RecipeCacheInvalidator(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task InvalidateAsync(Guid recipeId, string? slug, Guid categoryId, string? categorySlug, CancellationToken cancellationToken = default)
    {
        await _cache.SetStringAsync("recipes:cache:generation", Guid.NewGuid().ToString("N"), cancellationToken);
        await _cache.SetStringAsync("recipes:search:generation", Guid.NewGuid().ToString("N"), cancellationToken);
        await _cache.SetStringAsync("categories:cache:generation", Guid.NewGuid().ToString("N"), cancellationToken);
        await _cache.RemoveAsync($"recipes:{recipeId}", cancellationToken);
        if (!string.IsNullOrWhiteSpace(slug))
        {
            await _cache.RemoveAsync($"recipes:slug:{slug}", cancellationToken);
        }

        await _cache.RemoveAsync("categories:all", cancellationToken);
        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            await _cache.RemoveAsync($"categories:{categorySlug}", cancellationToken);
        }

        await _cache.RemoveAsync($"categories:id:{categoryId}", cancellationToken);
        await _cache.RemoveAsync("sitemap:xml", cancellationToken);
    }
}
