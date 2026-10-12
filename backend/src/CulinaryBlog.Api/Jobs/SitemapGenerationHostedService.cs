using CulinaryBlog.Application.Interfaces;

namespace CulinaryBlog.Api.Jobs;

public sealed class SitemapGenerationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SitemapGenerationHostedService> _logger;

    public SitemapGenerationHostedService(IServiceScopeFactory scopeFactory, ILogger<SitemapGenerationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var generator = scope.ServiceProvider.GetRequiredService<ISitemapGenerator>();
                await generator.GenerateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Sitemap generation failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
