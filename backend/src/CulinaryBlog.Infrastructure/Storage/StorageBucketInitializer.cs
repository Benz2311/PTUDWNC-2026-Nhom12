using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Storage;

internal sealed class StorageBucketInitializer(
    IConfiguration configuration, ILogger<StorageBucketInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = configuration.GetRequiredSection(StorageSettings.Section).Get<StorageSettings>()!;
        using var client = new AmazonS3Client(new BasicAWSCredentials(settings.AccessKey, settings.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = settings.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = settings.Region,
                Timeout = TimeSpan.FromSeconds(5),
            });

        var bucket = settings.DefaultBucket;
        var createdHere = false;
        using var retry = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                var buckets = await client.ListBucketsAsync(stoppingToken);
                if (buckets.Buckets?.Any(item => item.BucketName == bucket) != true)
                {
                    try
                    {
                        await client.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, stoppingToken);
                        createdHere = true;
                    }
                    catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou")
                    {
                        // Another instance owns initialization of this bucket.
                        return;
                    }
                }

                if (createdHere)
                {
                    var policy = JsonSerializer.Serialize(new
                    {
                        Version = "2012-10-17",
                        Statement = new[]
                        {
                            new { Effect = "Allow", Principal = "*", Action = "s3:GetObject", Resource = $"arn:aws:s3:::{bucket}/recipes/*" },
                        },
                    });
                    await client.PutBucketPolicyAsync(new PutBucketPolicyRequest
                    {
                        BucketName = bucket,
                        Policy = policy,
                    }, stoppingToken);
                    logger.LogInformation("Initialized recipe media bucket {BucketName}.", bucket);
                }

                return;
            }
            catch (AmazonS3Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Could not initialize storage bucket {BucketName} ({ErrorCode}); will retry.",
                    bucket, ex.ErrorCode);
            }
            catch (HttpRequestException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Could not reach storage while initializing {BucketName}; will retry.", bucket);
            }
        }
        while (await retry.WaitForNextTickAsync(stoppingToken));
    }
}
