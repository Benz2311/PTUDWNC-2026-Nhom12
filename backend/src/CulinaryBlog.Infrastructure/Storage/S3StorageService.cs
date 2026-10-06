using System.Net;
using System.Net.Sockets;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Storage;

/// <summary>
/// Dịch vụ lưu trữ tệp (Object Storage) sử dụng AWSSDK.S3 để giao tiếp với MinIO.
/// Tuân thủ SRS: "SRS dùng AWS SDK để nói chuyện với MinIO, không phải package Minio".
/// Hỗ trợ kiểm tra bảo mật (chống Path Traversal), cơ chế lũy tiến (Exponential Backoff Retry),
/// Idempotent Delete và ánh xạ lỗi sang StorageUnavailableException (HTTP 503).
/// </summary>
public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageSettings _settings;
    private readonly ILogger<S3StorageService>? _logger;

    public S3StorageService(IConfiguration configuration, ILogger<S3StorageService>? logger = null)
    {
        _logger = logger;
        _settings = configuration
            .GetSection(StorageSettings.Section)
            .Get<StorageSettings>()
            ?? new StorageSettings();

        var credentials = new BasicAWSCredentials(
            _settings.AccessKey,
            _settings.SecretKey);

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            ForcePathStyle = true,       // MinIO yêu cầu path-style addressing
            AuthenticationRegion = _settings.Region
        };

        _s3Client = new AmazonS3Client(credentials, config);
    }

    /// <summary>
    /// Constructor cho phép tiêm IAmazonS3 trực tiếp (phục vụ Unit Testing và Mocking).
    /// </summary>
    public S3StorageService(IAmazonS3 s3Client, StorageSettings settings, ILogger<S3StorageService>? logger = null)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var sanitizedBucket = ValidateBucketName(bucketName);
        var sanitizedKey = StoragePathHelper.SanitizeKey(objectKey);

        var resolvedContentType = string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType;

        var initialPosition = content.CanSeek ? content.Position : 0L;

        await ExecuteWithRetryAsync(
            async () =>
            {
                if (content.CanSeek && content.Position != initialPosition)
                {
                    content.Position = initialPosition;
                }

                var request = new PutObjectRequest
                {
                    BucketName = sanitizedBucket,
                    Key = sanitizedKey,
                    InputStream = content,
                    ContentType = resolvedContentType,
                    AutoCloseStream = false
                };

                return await _s3Client.PutObjectAsync(request, ct);
            },
            operationName: "Upload",
            sanitizedBucket,
            sanitizedKey,
            ct);

        return $"{_settings.ServiceUrl.TrimEnd('/')}/{sanitizedBucket}/{sanitizedKey}";
    }

    public Task<string> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        return UploadAsync(_settings.DefaultBucket, objectKey, content, contentType, ct);
    }

    public async Task DeleteAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        var sanitizedBucket = ValidateBucketName(bucketName);
        var sanitizedKey = StoragePathHelper.SanitizeKey(objectKey);

        await ExecuteWithRetryAsync<bool>(
            async () =>
            {
                try
                {
                    var request = new DeleteObjectRequest
                    {
                        BucketName = sanitizedBucket,
                        Key = sanitizedKey
                    };

                    await _s3Client.DeleteObjectAsync(request, ct);
                    return true;
                }
                catch (AmazonS3Exception ex) when (IsObjectNotFound(ex))
                {
                    // Idempotent: Nếu object đã không tồn tại trên MinIO, coi như trạng thái mong muốn đã đạt được
                    _logger?.LogDebug("Object '{Key}' không tồn tại trong bucket '{Bucket}', thao tác Delete coi như thành công (idempotent).", sanitizedKey, sanitizedBucket);
                    return true;
                }
            },
            operationName: "Delete",
            sanitizedBucket,
            sanitizedKey,
            ct);
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        return DeleteAsync(_settings.DefaultBucket, objectKey, ct);
    }

    public async Task<string> GetPresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var sanitizedBucket = ValidateBucketName(bucketName);
        var sanitizedKey = StoragePathHelper.SanitizeKey(objectKey);

        return await ExecuteWithRetryAsync(
            async () =>
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = sanitizedBucket,
                    Key = sanitizedKey,
                    Expires = DateTime.UtcNow.Add(expiry),
                    Verb = HttpVerb.GET
                };

                return await _s3Client.GetPreSignedURLAsync(request);
            },
            operationName: "GetPresignedUrl",
            sanitizedBucket,
            sanitizedKey,
            ct);
    }

    private static string ValidateBucketName(string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new ArgumentException("Tên bucket không được để trống.", nameof(bucketName));
        }

        var trimmed = bucketName.Trim();
        if (trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains(".."))
        {
            throw new ArgumentException($"Tên bucket không hợp lệ: '{bucketName}'.", nameof(bucketName));
        }

        return trimmed;
    }

    private static bool IsObjectNotFound(AmazonS3Exception ex)
    {
        return ex.StatusCode == HttpStatusCode.NotFound
            || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTransientError(Exception ex)
    {
        if (ex is AmazonS3Exception s3Ex)
        {
            var code = (int)s3Ex.StatusCode;
            if (code >= 500 && code <= 599)
            {
                return true;
            }

            if (s3Ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                return true;
            }

            if (string.Equals(s3Ex.ErrorCode, "RequestTimeout", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s3Ex.ErrorCode, "ServiceUnavailable", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s3Ex.ErrorCode, "SlowDown", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return ex is HttpRequestException
            || ex is SocketException
            || ex is TimeoutException;
    }

    private static bool IsPermanentError(Exception ex)
    {
        if (ex is AmazonS3Exception s3Ex)
        {
            if (s3Ex.StatusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound)
            {
                return true;
            }

            if (string.Equals(s3Ex.ErrorCode, "InvalidAccessKeyId", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s3Ex.ErrorCode, "SignatureDoesNotMatch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s3Ex.ErrorCode, "AccessDenied", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return ex is ArgumentException;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        string bucketName,
        string objectKey,
        CancellationToken ct)
    {
        var maxRetries = Math.Max(1, _settings.MaxRetries);
        var baseDelayMs = Math.Max(10, _settings.BaseDelayMs);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await operation();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsPermanentError(ex))
            {
                _logger?.LogError(ex, "Thao tác Storage {Operation} gặp lỗi không thể retry (Permanent Error): Bucket '{Bucket}', Key '{Key}'.",
                    operationName, bucketName, objectKey);
                throw;
            }
            catch (Exception ex) when (IsTransientError(ex))
            {
                lastException = ex;
                if (attempt < maxRetries)
                {
                    var delayMs = (int)(baseDelayMs * Math.Pow(2, attempt - 1));
                    _logger?.LogWarning(ex, "Thao tác Storage {Operation} tạm thời thất bại (Lần thử {Attempt}/{MaxRetries}). Sẽ thử lại sau {Delay}ms. Bucket: '{Bucket}', Key: '{Key}'.",
                        operationName, attempt, maxRetries, delayMs, bucketName, objectKey);

                    await Task.Delay(delayMs, ct);
                }
            }
            catch (ObjectDisposedException) when (lastException != null)
            {
                _logger?.LogWarning("Luồng dữ liệu đã bị đóng sau sự cố kết nối ở lần thử trước ({Operation}). Dừng retry.", operationName);
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Thao tác Storage {Operation} thất bại với lỗi không xác định. Bucket '{Bucket}', Key '{Key}'.",
                    operationName, bucketName, objectKey);
                throw new StorageException($"Lỗi dịch vụ lưu trữ khi thực hiện '{operationName}' trên bucket '{bucketName}', key '{objectKey}': {ex.Message}", ex);
            }
        }

        _logger?.LogError(lastException, "Thao tác Storage {Operation} kiệt số lần retry ({MaxRetries} lần). Dịch vụ MinIO/S3 không khả dụng. Bucket: '{Bucket}', Key: '{Key}'.",
            operationName, maxRetries, bucketName, objectKey);

        throw new StorageUnavailableException(
            $"Dịch vụ lưu trữ tệp (MinIO) tạm thời không khả dụng sau {maxRetries} lần thử ({operationName}: {bucketName}/{objectKey}).",
            lastException!);
    }
}
