using Amazon.Runtime;
using Amazon.S3;
using CulinaryBlog.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Infrastructure.HealthChecks;

/// <summary>
/// Health check kiểm tra khả năng kết nối thực tế tới MinIO / S3 Object Storage qua AWSSDK.S3.
/// Không lộ access key hoặc secret key.
/// </summary>
public class MinioHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public MinioHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = _configuration
            .GetSection(StorageSettings.Section)
            .Get<StorageSettings>();

        if (settings == null || string.IsNullOrWhiteSpace(settings.ServiceUrl))
        {
            return HealthCheckResult.Unhealthy("MinIO storage settings are not configured.");
        }

        try
        {
            var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = settings.ServiceUrl,
                ForcePathStyle = true,
                Timeout = TimeSpan.FromSeconds(3),
                MaxErrorRetry = 1
            };

            using var s3Client = new AmazonS3Client(credentials, config);
            var response = await s3Client.ListBucketsAsync(cancellationToken);

            return HealthCheckResult.Healthy($"MinIO is operational. Buckets count: {response.Buckets.Count}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"MinIO check failed: {ex.Message}");
        }
    }
}
