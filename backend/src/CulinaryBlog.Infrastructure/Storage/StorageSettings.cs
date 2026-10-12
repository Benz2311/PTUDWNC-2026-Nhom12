namespace CulinaryBlog.Infrastructure.Storage;

/// <summary>
/// Cấu hình kết nối và tham số vận hành cho Object Storage (MinIO / S3).
/// </summary>
public sealed class StorageSettings
{
    public const string Section = "Storage";

    public string ServiceUrl { get; init; } = "http://localhost:9000";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Region { get; init; } = "us-east-1";
    public string DefaultBucket { get; init; } = "culinaryblog";
    public int MaxRetries { get; init; } = 3;
    public int BaseDelayMs { get; init; } = 100;
}
