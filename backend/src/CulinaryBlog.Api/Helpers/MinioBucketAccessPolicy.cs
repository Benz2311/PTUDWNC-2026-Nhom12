using System.Text.Json;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Api.Helpers;

public static class MinioBucketAccessPolicy
{
    public static async Task EnsurePublicReadAsync(
        IMinioClient minio,
        string bucketName,
        CancellationToken cancellationToken)
    {
        var bucketExists = await minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName), cancellationToken);
        if (!bucketExists)
        {
            await minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName), cancellationToken);
        }

        await minio.SetPolicyAsync(
            new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(CreatePublicReadPolicy(bucketName)), cancellationToken);
    }

    private static string CreatePublicReadPolicy(string bucketName) => JsonSerializer.Serialize(new
    {
        Version = "2012-10-17",
        Statement = new[]
        {
            new
            {
                Effect = "Allow",
                Principal = new { AWS = new[] { "*" } },
                Action = new[] { "s3:GetObject" },
                Resource = new[] { $"arn:aws:s3:::{bucketName}/*" }
            }
        }
    });
}
