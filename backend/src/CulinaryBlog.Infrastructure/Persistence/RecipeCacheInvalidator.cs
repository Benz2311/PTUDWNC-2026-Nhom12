using System.Text.Json;
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
        var options = new DistributedCacheEntryOptions();
        await SetGenerationAsync("recipes:cache:generation", options, cancellationToken);
        await SetGenerationAsync("recipes:search:generation", options, cancellationToken);
        await _cache.SetStringAsync("categories:cache:generation", Guid.NewGuid().ToString("N"), cancellationToken);
        await _cache.RemoveAsync($"recipes:{recipeId}", cancellationToken);
        if (!string.IsNullOrWhiteSpace(slug))
        {
            await _cache.RemoveAsync($"recipe:{slug.Trim().ToLowerInvariant()}", cancellationToken);
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

    private Task SetGenerationAsync(string key, DistributedCacheEntryOptions options, CancellationToken cancellationToken) =>
        _cache.SetAsync(
            key,
            JsonSerializer.SerializeToUtf8Bytes(Guid.NewGuid().ToString("N")),
            options,
            cancellationToken);
}
