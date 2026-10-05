using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CulinaryBlog.UnitTests.Infrastructure.Storage;

public class S3StorageServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly Mock<ILogger<S3StorageService>> _loggerMock;
    private readonly StorageSettings _settings;

    public S3StorageServiceTests()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _loggerMock = new Mock<ILogger<S3StorageService>>();
        _settings = new StorageSettings
        {
            ServiceUrl = "http://localhost:9000",
            AccessKey = "test-access-key-SUPERSECRET123",
            SecretKey = "test-secret-key-TOPSECRET456",
            Region = "us-east-1",
            DefaultBucket = "culinaryblog",
            MaxRetries = 3,
            BaseDelayMs = 10 // Đặt delay nhỏ để unit test chạy tức thì
        };
    }

    private S3StorageService CreateService()
    {
        return new S3StorageService(_s3ClientMock.Object, _settings, _loggerMock.Object);
    }

    [Fact]
    public async Task UploadAsync_WithValidStream_UploadsSuccessfullyAndReturnsPublicUrl()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream("Fake Image Bytes"u8.ToArray());
        var objectKey = "recipes/123/images/pho-bo-1.jpg";

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        var url = await service.UploadAsync(objectKey, stream, "image/jpeg");

        // Assert
        url.Should().Be("http://localhost:9000/culinaryblog/recipes/123/images/pho-bo-1.jpg");

        _s3ClientMock.Verify(x => x.PutObjectAsync(
            It.Is<PutObjectRequest>(r =>
                r.BucketName == "culinaryblog" &&
                r.Key == objectKey &&
                r.ContentType == "image/jpeg" &&
                r.DisablePayloadSigning == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("../../secret.txt")]
    [InlineData("recipes/../../evil.jpg")]
    [InlineData("/root/passwords.txt")]
    public async Task UploadAsync_WithPathTraversalInKey_ThrowsArgumentException(string maliciousKey)
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream("Test"u8.ToArray());

        // Act
        var act = () => service.UploadAsync(maliciousKey, stream, "image/jpeg");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Path Traversal*");

        _s3ClientMock.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("../../secret.txt")]
    [InlineData("..\\windows\\system.ini")]
    public async Task DeleteAsync_WithPathTraversalInKey_ThrowsArgumentException(string maliciousKey)
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.DeleteAsync(maliciousKey);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Path Traversal*");

        _s3ClientMock.Verify(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenObjectExists_CallsDeleteObjectAsyncSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var objectKey = "recipes/123/images/pho-bo.jpg";

        _s3ClientMock
            .Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

        // Act
        await service.DeleteAsync(objectKey);

        // Assert
        _s3ClientMock.Verify(x => x.DeleteObjectAsync(
            It.Is<DeleteObjectRequest>(r => r.BucketName == "culinaryblog" && r.Key == objectKey),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenObjectDoesNotExist_IsIdempotentAndDoesNotThrow()
    {
        // Arrange
        var service = CreateService();
        var objectKey = "recipes/123/images/non-existent.jpg";

        // Giả lập MinIO trả lỗi NotFound 404 (hoặc NoSuchKey)
        var notFoundEx = new AmazonS3Exception("The specified key does not exist.")
        {
            StatusCode = HttpStatusCode.NotFound,
            ErrorCode = "NoSuchKey"
        };

        _s3ClientMock
            .Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundEx);

        // Act & Assert (Không ném ngoại lệ vì delete mang tính idempotent)
        var act = () => service.DeleteAsync(objectKey);
        await act.Should().NotThrowAsync();

        _s3ClientMock.Verify(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WhenTransientErrorOccursThenSucceeds_RetriesAndReturnsUrl()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream("Transient Content"u8.ToArray());
        var objectKey = "recipes/123/images/retry-test.jpg";

        var transientEx = new AmazonS3Exception("MinIO Service Unavailable")
        {
            StatusCode = HttpStatusCode.ServiceUnavailable,
            ErrorCode = "ServiceUnavailable"
        };

        var callCount = 0;
        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    throw transientEx;
                }
                return Task.FromResult(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });
            });

        // Act
        var url = await service.UploadAsync(objectKey, stream, "image/jpeg");

        // Assert
        url.Should().Be("http://localhost:9000/culinaryblog/recipes/123/images/retry-test.jpg");
        callCount.Should().Be(2);
        _s3ClientMock.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UploadAsync_WhenTransientErrorPersists_StopsAtMaxRetriesAndThrowsStorageUnavailableException()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream("Transient Failure Content"u8.ToArray());
        var objectKey = "recipes/123/images/persistent-fail.jpg";

        var transientEx = new AmazonS3Exception("MinIO Internal Error 500")
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(transientEx);

        // Act
        var act = () => service.UploadAsync(objectKey, stream, "image/jpeg");

        // Assert
        await act.Should().ThrowAsync<StorageUnavailableException>()
            .WithMessage("*tạm thời không khả dụng sau 3 lần thử*");

        // Đảm bảo chỉ retry đúng 3 lần (MaxRetries = 3), không retry vô hạn
        _s3ClientMock.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task UploadAsync_WhenPermanentErrorOccurs_DoesNotRetryAndThrowsImmediately()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream("Permanent Failure"u8.ToArray());
        var objectKey = "recipes/123/images/forbidden.jpg";

        var permanentEx = new AmazonS3Exception("Access Denied")
        {
            StatusCode = HttpStatusCode.Forbidden,
            ErrorCode = "AccessDenied"
        };

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(permanentEx);

        // Act
        var act = () => service.UploadAsync(objectKey, stream, "image/jpeg");

        // Assert: Ném ngay lập tức, không retry
        await act.Should().ThrowAsync<AmazonS3Exception>();
        _s3ClientMock.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WhenMinIOUnavailableDueToNetwork_ThrowsStorageUnavailableException()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream("Network Error Content"u8.ToArray());
        var objectKey = "recipes/123/images/network-fail.jpg";

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("No connection could be made because the target machine actively refused it."));

        // Act
        var act = () => service.UploadAsync(objectKey, stream, "image/jpeg");

        // Assert
        await act.Should().ThrowAsync<StorageUnavailableException>()
            .WithMessage("*tạm thời không khả dụng*");

        _s3ClientMock.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task UploadAsync_WhenFails_NeverLeaksSecretCredentialsInException()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream("Test"u8.ToArray());
        var objectKey = "recipes/123/images/credential-test.jpg";

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection timeout"));

        // Act
        var act = () => service.UploadAsync(objectKey, stream, "image/jpeg");

        // Assert
        var ex = await act.Should().ThrowAsync<StorageUnavailableException>();
        ex.Which.Message.Should().NotContain(_settings.AccessKey);
        ex.Which.Message.Should().NotContain(_settings.SecretKey);
        ex.Which.ToString().Should().NotContain(_settings.SecretKey);
    }

    [Fact]
    public async Task UploadAsync_WhenCancellationTokenCanceled_StopsImmediatelyWithoutRetry()
    {
        // Arrange
        var service = CreateService();
        using var stream = new MemoryStream("Cancel Test"u8.ToArray());
        var objectKey = "recipes/123/images/cancel.jpg";

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Đã cancel từ trước

        // Act
        var act = () => service.UploadAsync(objectKey, stream, "image/jpeg", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _s3ClientMock.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
