namespace CulinaryBlog.Application.Interfaces;

/// <summary>
/// Object storage service interface — implemented by S3StorageService (AWSSDK.S3 → MinIO) in Infrastructure.
/// </summary>
public interface IStorageService
{
    /// <summary>Upload file và trả về public URL.</summary>
    Task<string> UploadAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct = default);

    Task<string> GetPresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiry,
        CancellationToken ct = default);

    /// <summary>Upload file sử dụng bucket mặc định được cấu hình.</summary>
    Task<string> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    /// <summary>Xóa file sử dụng bucket mặc định được cấu hình (Idempotent).</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
