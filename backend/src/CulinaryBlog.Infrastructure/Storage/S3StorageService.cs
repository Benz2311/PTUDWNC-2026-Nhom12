using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.Infrastructure.Storage;

/// <summary>
/// Object storage service dùng AWSSDK.S3 để kết nối MinIO.
/// Theo SRS: "SRS dùng AWS SDK để nói chuyện với MinIO, không phải package Minio".
/// </summary>
public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageSettings _settings;

    public S3StorageService(IConfiguration configuration)
    {
        _settings = configuration
            .GetSection(StorageSettings.Section)
            .Get<StorageSettings>()
            ?? throw new InvalidOperationException("Storage settings are not configured.");

        var credentials = new BasicAWSCredentials(
            _settings.AccessKey,
            _settings.SecretKey);

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            ForcePathStyle = true,       // MinIO yêu cầu path-style
            AuthenticationRegion = _settings.Region
        };

        _s3Client = new AmazonS3Client(credentials, config);
    }

    public async Task<string> UploadAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true  // MinIO compatibility
        };

        await _s3Client.PutObjectAsync(request, ct);

        return $"{_settings.ServiceUrl}/{bucketName}/{objectKey}";
    }

    public async Task DeleteAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(request, ct);
    }

    public async Task<string> GetPresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        return await _s3Client.GetPreSignedURLAsync(request);
    }
}

public sealed class StorageSettings
{
    public const string Section = "Storage";

    public string ServiceUrl { get; init; } = "http://localhost:9000";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Region { get; init; } = "us-east-1";
    public string DefaultBucket { get; init; } = "culinaryblog";
}
