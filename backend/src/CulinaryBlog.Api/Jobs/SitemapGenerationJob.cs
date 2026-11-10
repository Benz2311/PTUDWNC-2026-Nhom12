using CulinaryBlog.Application.Interfaces;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Api.Jobs;

public sealed class SitemapGenerationJob(
    ISitemapGenerator generator,
    IConfiguration configuration,
    ILogger<SitemapGenerationJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting scheduled sitemap generation job...");
        var xml = await generator.GenerateAsync(cancellationToken);
        logger.LogInformation("Sitemap generated successfully. Length: {Length} chars.", xml.Length);

        // Search engine notification/ping (SRS Section 5.3)
        var pingUrlTemplate = configuration["GoogleSearch:PingUrlTemplate"];
        if (!string.IsNullOrWhiteSpace(pingUrlTemplate))
        {
            var publicOrigin = configuration["Site:PublicBaseUrl"] ?? "http://localhost:8088";
            var sitemapUrl = $"{publicOrigin.TrimEnd('/')}/sitemap.xml";
            var pingUrl = string.Format(pingUrlTemplate, Uri.EscapeDataString(sitemapUrl));
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                logger.LogInformation("Pinging search engine at: {PingUrl}", pingUrl);
                var response = await client.GetAsync(pingUrl, cancellationToken);
                logger.LogInformation("Search engine ping status: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Search engine ping failed (non-fatal, sitemap remains valid).");
            }
        }
    }
}
