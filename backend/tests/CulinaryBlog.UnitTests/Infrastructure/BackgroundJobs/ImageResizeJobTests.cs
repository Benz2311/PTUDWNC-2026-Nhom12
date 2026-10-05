using System.Reflection;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.Persistence;
using FluentAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace CulinaryBlog.UnitTests.Infrastructure.BackgroundJobs;

public class ImageResizeJobTests : IDisposable
{
    private readonly TestApplicationDbContext _dbContext;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<ILogger<ImageResizeJob>> _loggerMock;
    private readonly ImageResizeJob _job;

    public ImageResizeJobTests()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"CulinaryBlog_Test_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new TestApplicationDbContext(options);
        _storageServiceMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<ImageResizeJob>>();

        _job = new ImageResizeJob(
            _dbContext,
            _storageServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestApplicationDbContext : DbContext, IApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
        public DbSet<Recipe> Recipes => Set<Recipe>();
        public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
        public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>(null!);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecipeImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Ignore(e => e.Recipe);
            });

            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.OwnsOne(r => r.Nutrition);
            });

            modelBuilder.Ignore<RecipeStep>();
            modelBuilder.Ignore<RecipeIngredient>();
            modelBuilder.Ignore<Category>();
            modelBuilder.Ignore<ApplicationUser>();
        }
    }

    private static byte[] CreateTestImageBytes(int width, int height, IImageFormat format)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        image.Save(ms, format);
        return ms.ToArray();
    }

    [Fact]
    public async Task ProcessAsync_WithValidJpegImage_ResizesMediumAndThumbnailAndUpdatesDatabase()
    {
        // Arrange: Tạo ảnh JPEG 1200x900 thật
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var rawKey = $"recipes/{recipeId}/images/{imageId}.jpg";
        var originalUrl = $"http://localhost:9000/culinaryblog/{rawKey}";

        var recipeImage = new RecipeImage
        {
            Id = imageId,
            RecipeId = recipeId,
            OriginalUrl = originalUrl,
            MediumUrl = null,
            ThumbnailUrl = null,
            IsDeleted = false,
        };

        _dbContext.RecipeImages.Add(recipeImage);
        await _dbContext.SaveChangesAsync();

        var jpegBytes = CreateTestImageBytes(1200, 900, JpegFormat.Instance);
        _storageServiceMock
            .Setup(x => x.DownloadAsync("culinaryblog", rawKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(jpegBytes));

        var expectedMediumUrl = $"http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{imageId}_medium.jpg";
        var expectedThumbUrl = $"http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{imageId}_thumb.jpg";

        _storageServiceMock
            .Setup(x => x.UploadAsync(
                "culinaryblog",
                $"recipes/{recipeId}/images/{imageId}_medium.jpg",
                It.IsAny<Stream>(),
                "image/jpeg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMediumUrl);

        _storageServiceMock
            .Setup(x => x.UploadAsync(
                "culinaryblog",
                $"recipes/{recipeId}/images/{imageId}_thumb.jpg",
                It.IsAny<Stream>(),
                "image/jpeg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedThumbUrl);

        // Act
        await _job.ProcessAsync(imageId);

        // Assert
        var updatedImage = await _dbContext.RecipeImages.FindAsync(imageId);
        updatedImage.Should().NotBeNull();
        updatedImage!.OriginalUrl.Should().Be(originalUrl, "OriginalUrl tuyệt đối không được thay đổi");
        updatedImage.MediumUrl.Should().Be(expectedMediumUrl);
        updatedImage.ThumbnailUrl.Should().Be(expectedThumbUrl);
        updatedImage.UpdatedAt.Should().NotBeNull();

        _storageServiceMock.Verify(x => x.UploadAsync(
            "culinaryblog",
            $"recipes/{recipeId}/images/{imageId}_medium.jpg",
            It.IsAny<Stream>(),
            "image/jpeg",
            It.IsAny<CancellationToken>()), Times.Once);

        _storageServiceMock.Verify(x => x.UploadAsync(
            "culinaryblog",
            $"recipes/{recipeId}/images/{imageId}_thumb.jpg",
            It.IsAny<Stream>(),
            "image/jpeg",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WithValidPngImage_PreservesPngFormatAndResizesCorrectly()
    {
        // Arrange: Tạo ảnh PNG 1000x500 thật
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var rawKey = $"recipes/{recipeId}/images/{imageId}.png";
        var originalUrl = $"http://localhost:9000/culinaryblog/{rawKey}";

        var recipeImage = new RecipeImage
        {
            Id = imageId,
            RecipeId = recipeId,
            OriginalUrl = originalUrl,
            MediumUrl = null,
            ThumbnailUrl = null,
            IsDeleted = false,
        };

        _dbContext.RecipeImages.Add(recipeImage);
        await _dbContext.SaveChangesAsync();

        var pngBytes = CreateTestImageBytes(1000, 500, PngFormat.Instance);
        _storageServiceMock
            .Setup(x => x.DownloadAsync("culinaryblog", rawKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(pngBytes));

        _storageServiceMock
            .Setup(x => x.UploadAsync(
                "culinaryblog",
                $"recipes/{recipeId}/images/{imageId}_medium.png",
                It.IsAny<Stream>(),
                "image/png",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{imageId}_medium.png");

        _storageServiceMock
            .Setup(x => x.UploadAsync(
                "culinaryblog",
                $"recipes/{recipeId}/images/{imageId}_thumb.png",
                It.IsAny<Stream>(),
                "image/png",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{imageId}_thumb.png");

        // Act
        await _job.ProcessAsync(imageId);

        // Assert
        var updatedImage = await _dbContext.RecipeImages.FindAsync(imageId);
        updatedImage.Should().NotBeNull();
        updatedImage!.MediumUrl.Should().EndWith("_medium.png");
        updatedImage.ThumbnailUrl.Should().EndWith("_thumb.png");
    }

    [Fact]
    public async Task ProcessAsync_WithValidWebpImage_PreservesWebpFormatAndResizesCorrectly()
    {
        // Arrange: Tạo ảnh WebP 1600x1200 thật
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var rawKey = $"recipes/{recipeId}/images/{imageId}.webp";
        var originalUrl = $"http://localhost:9000/culinaryblog/{rawKey}";

        var recipeImage = new RecipeImage
        {
            Id = imageId,
            RecipeId = recipeId,
            OriginalUrl = originalUrl,
            MediumUrl = null,
            ThumbnailUrl = null,
            IsDeleted = false,
        };

        _dbContext.RecipeImages.Add(recipeImage);
        await _dbContext.SaveChangesAsync();

        var webpBytes = CreateTestImageBytes(1600, 1200, WebpFormat.Instance);
        _storageServiceMock
            .Setup(x => x.DownloadAsync("culinaryblog", rawKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(webpBytes));

        _storageServiceMock
            .Setup(x => x.UploadAsync(
                "culinaryblog",
                $"recipes/{recipeId}/images/{imageId}_medium.webp",
                It.IsAny<Stream>(),
                "image/webp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{imageId}_medium.webp");

        _storageServiceMock
            .Setup(x => x.UploadAsync(
                "culinaryblog",
                $"recipes/{recipeId}/images/{imageId}_thumb.webp",
                It.IsAny<Stream>(),
                "image/webp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{imageId}_thumb.webp");

        // Act
        await _job.ProcessAsync(imageId);

        // Assert
        var updatedImage = await _dbContext.RecipeImages.FindAsync(imageId);
        updatedImage.Should().NotBeNull();
        updatedImage!.MediumUrl.Should().EndWith("_medium.webp");
        updatedImage.ThumbnailUrl.Should().EndWith("_thumb.webp");
    }

    [Fact]
    public async Task ProcessAsync_WhenImageIsCorrupt_ThrowsExceptionAndDoesNotUpdateDatabaseWithFakeUrls()
    {
        // Arrange: File nhị phân bị hỏng (corrupt bytes)
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var rawKey = $"recipes/{recipeId}/images/{imageId}.jpg";

        var recipeImage = new RecipeImage
        {
            Id = imageId,
            RecipeId = recipeId,
            OriginalUrl = $"http://localhost:9000/culinaryblog/{rawKey}",
            MediumUrl = null,
            ThumbnailUrl = null,
            IsDeleted = false,
        };

        _dbContext.RecipeImages.Add(recipeImage);
        await _dbContext.SaveChangesAsync();

        var corruptBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };
        _storageServiceMock
            .Setup(x => x.DownloadAsync("culinaryblog", rawKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(corruptBytes));

        // Act
        var act = () => _job.ProcessAsync(imageId);

        // Assert
        await act.Should().ThrowAsync<Exception>();

        var imageInDb = await _dbContext.RecipeImages.FindAsync(imageId);
        imageInDb!.MediumUrl.Should().BeNull("Không được ghi URL giả khi decode ảnh thất bại");
        imageInDb.ThumbnailUrl.Should().BeNull("Không được ghi URL giả khi decode ảnh thất bại");

        _storageServiceMock.Verify(x => x.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenStorageUploadFails_ThrowsExceptionAndRollbacksDatabaseState()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var rawKey = $"recipes/{recipeId}/images/{imageId}.jpg";

        var recipeImage = new RecipeImage
        {
            Id = imageId,
            RecipeId = recipeId,
            OriginalUrl = $"http://localhost:9000/culinaryblog/{rawKey}",
            MediumUrl = null,
            ThumbnailUrl = null,
            IsDeleted = false,
        };

        _dbContext.RecipeImages.Add(recipeImage);
        await _dbContext.SaveChangesAsync();

        var jpegBytes = CreateTestImageBytes(400, 300, JpegFormat.Instance);
        _storageServiceMock
            .Setup(x => x.DownloadAsync("culinaryblog", rawKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(jpegBytes));

        // Giả lập MinIO upload thất bại
        _storageServiceMock
            .Setup(x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("MinIO Connection Timeout"));

        // Act
        var act = () => _job.ProcessAsync(imageId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();

        var imageInDb = await _dbContext.RecipeImages.FindAsync(imageId);
        imageInDb!.MediumUrl.Should().BeNull();
        imageInDb.ThumbnailUrl.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_WhenRecipeImageDoesNotExist_ExitsGracefullyWithoutThrowing()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _job.ProcessAsync(nonExistentId);

        // Assert
        await act.Should().NotThrowAsync();
        _storageServiceMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenRecipeImageIsSoftDeleted_ExitsGracefullyWithoutResizing()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        var recipeImage = new RecipeImage
        {
            Id = imageId,
            RecipeId = recipeId,
            OriginalUrl = $"http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{imageId}.jpg",
            IsDeleted = true, // Đã xóa mềm
        };

        _dbContext.RecipeImages.Add(recipeImage);
        await _dbContext.SaveChangesAsync();

        // Act
        await _job.ProcessAsync(imageId);

        // Assert
        _storageServiceMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenAlreadyHasMediumAndThumbnail_IsIdempotentAndSkipsReprocessing()
    {
        // Arrange: Ảnh đã có sẵn MediumUrl và ThumbnailUrl
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        var recipeImage = new RecipeImage
        {
            Id = imageId,
            RecipeId = recipeId,
            OriginalUrl = $"http://localhost:9000/culinaryblog/recipes/{recipeId}/images/{imageId}.jpg",
            MediumUrl = "http://localhost:9000/culinaryblog/recipes/existing_medium.jpg",
            ThumbnailUrl = "http://localhost:9000/culinaryblog/recipes/existing_thumb.jpg",
            IsDeleted = false,
        };

        _dbContext.RecipeImages.Add(recipeImage);
        await _dbContext.SaveChangesAsync();

        // Act
        await _job.ProcessAsync(imageId);

        // Assert: Không tải hoặc xử lý lại
        _storageServiceMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageServiceMock.Verify(x => x.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ImageResizeJob_HasAutomaticRetryAttributeWith3Attempts()
    {
        // Arrange & Act
        var method = typeof(ImageResizeJob).GetMethod(nameof(ImageResizeJob.ProcessAsync));
        var attribute = method?.GetCustomAttribute<AutomaticRetryAttribute>();

        // Assert: Kiểm tra cấu hình Hangfire Retry 3 lần theo yêu cầu Task 4
        attribute.Should().NotBeNull();
        attribute!.Attempts.Should().Be(3);
        attribute.OnAttemptsExceeded.Should().Be(AttemptsExceededAction.Fail);
    }

    [Fact]
    public async Task ProcessAsync_WhenCancellationRequested_CancelsImmediately()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Hủy ngay từ đầu

        // Act
        var act = () => _job.ProcessAsync(imageId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
