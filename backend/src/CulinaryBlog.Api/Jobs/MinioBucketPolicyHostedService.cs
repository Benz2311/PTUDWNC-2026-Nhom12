using CulinaryBlog.Api.Helpers;
using Minio;

namespace CulinaryBlog.Api.Jobs;

public sealed class MinioBucketPolicyHostedService : BackgroundService
{
    private readonly IMinioClient _minio;
    private readonly string _bucketName;
    private readonly ILogger<MinioBucketPolicyHostedService> _logger;

    public MinioBucketPolicyHostedService(
        IMinioClient minio,
        IConfiguration configuration,
        ILogger<MinioBucketPolicyHostedService> logger)
    {
        _minio = minio;
        _bucketName = configuration["Minio:BucketName"] ?? "culinary-blog";
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MinioBucketAccessPolicy.EnsurePublicReadAsync(_minio, _bucketName, stoppingToken);
                _logger.LogInformation("MinIO bucket {BucketName} is ready for public image reads.", _bucketName);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not configure MinIO bucket {BucketName}; retrying in 5 seconds.", _bucketName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
