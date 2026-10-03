using System.Xml.Linq;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class SitemapGenerator : ISitemapGenerator
{
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;

    public SitemapGenerator(ApplicationDbContext db, IDistributedCache cache, IConfiguration configuration)
    {
        _db = db;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = (_configuration["Site:PublicBaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var recipes = await _db.Recipes
            .AsNoTracking()
            .Where(recipe => !recipe.IsDeleted && recipe.Status == RecipeStatus.Published)
            .OrderBy(recipe => recipe.Slug)
            .Select(recipe => new { recipe.Slug, recipe.UpdatedAt, recipe.CreatedAt })
            .ToListAsync(cancellationToken);
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(category => !category.IsDeleted)
            .OrderBy(category => category.Slug)
            .Select(category => new { category.Slug, category.UpdatedAt, category.CreatedAt })
            .ToListAsync(cancellationToken);

        var urls = recipes.Select(recipe => new
            {
                Location = $"{baseUrl}/recipes/{Uri.EscapeDataString(recipe.Slug)}",
                LastModified = recipe.UpdatedAt ?? recipe.CreatedAt
            })
            .Concat(categories.Select(category => new
            {
                Location = $"{baseUrl}/categories/{Uri.EscapeDataString(category.Slug)}",
                LastModified = category.UpdatedAt ?? category.CreatedAt
            }));

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(SitemapNamespace + "urlset",
                urls.Select(item => new XElement(SitemapNamespace + "url",
                    new XElement(SitemapNamespace + "loc", item.Location),
                    new XElement(SitemapNamespace + "lastmod", item.LastModified.ToString("yyyy-MM-dd"))))));
        var xml = document.ToString(SaveOptions.DisableFormatting);
        await _cache.SetStringAsync("sitemap:xml", xml, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
        }, cancellationToken);
        return xml;
    }
}
