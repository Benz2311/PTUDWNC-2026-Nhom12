using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace CulinaryBlog.Api.Endpoints.Sitemap;

public static class SitemapEndpoints
{
    public static void MapSitemapEndpoints(this WebApplication app)
    {
        app.MapGet("/sitemap.xml", async (IDistributedCache cache, ISitemapGenerator generator, CancellationToken cancellationToken) =>
        {
            var xml = await cache.GetStringAsync("sitemap:xml", cancellationToken);
            xml ??= await generator.GenerateAsync(cancellationToken);
            return Results.Content(xml, "application/xml; charset=utf-8");
        }).AllowAnonymous();
    }
}
