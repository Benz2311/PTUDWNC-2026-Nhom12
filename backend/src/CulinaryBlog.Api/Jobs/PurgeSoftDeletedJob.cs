using CulinaryBlog.Application.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Api.Jobs;

public sealed class PurgeSoftDeletedJob(IRecipePurgeService purgeService, ILogger<PurgeSoftDeletedJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting daily purge job for soft-deleted content older than 30 days...");
        var count = await purgeService.PurgeExpiredAsync(cancellationToken);
        logger.LogInformation("Daily purge completed. Total expired recipes purged: {Count}", count);
    }
}
