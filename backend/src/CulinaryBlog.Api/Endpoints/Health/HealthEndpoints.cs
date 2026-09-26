using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using CulinaryBlog.Api.Contracts;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Api.Endpoints.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new ApiResponse<object>(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            checks = new[] { "live", "ready" }
        }))).WithTags("Health");

        app.MapGet("/health/live", () => Results.Ok(new ApiResponse<object>(new
        {
            status = "Alive",
            timestamp = DateTime.UtcNow
        }))).WithTags("Health");

        app.MapGet("/health/ready", async (ApplicationDbContext db, IDistributedCache cache, IMinioClient minio, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            try
            {
                var databaseReady = await db.Database.CanConnectAsync(cancellationToken);
                await cache.SetStringAsync("health:ready", "ok", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                }, cancellationToken);
                var redisReady = await cache.GetStringAsync("health:ready", cancellationToken) == "ok";
                var bucketName = configuration["Minio:BucketName"] ?? "culinary-blog";
                var minioReady = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), cancellationToken);
                return databaseReady && redisReady && minioReady
                    ? Results.Ok(new ApiResponse<object>(new
                    {
                        status = "Ready",
                        database = "PostgreSQL",
                        redis = "Ready",
                        minio = "Ready",
                        timestamp = DateTime.UtcNow
                    }))
                    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }).WithTags("Health");
    }
}
