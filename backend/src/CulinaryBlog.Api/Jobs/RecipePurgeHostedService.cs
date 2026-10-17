using CulinaryBlog.Application.Interfaces;

namespace CulinaryBlog.Api.Jobs;

public sealed class RecipePurgeHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecipePurgeHostedService> _logger;

    public RecipePurgeHostedService(IServiceScopeFactory scopeFactory, ILogger<RecipePurgeHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var purgeService = scope.ServiceProvider.GetRequiredService<IRecipePurgeService>();
                var count = await purgeService.PurgeExpiredAsync(stoppingToken);
                if (count > 0)
                {
                    _logger.LogInformation("Purged {RecipeCount} expired recipes.", count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Expired recipe purge job failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
