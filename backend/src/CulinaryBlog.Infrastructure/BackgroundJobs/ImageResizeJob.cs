using CulinaryBlog.Application.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

/// <summary>
/// Background Job xử lý tạo ảnh Medium và Thumbnail (JOB-002 Image Resize / Thumbnail).
/// Chạy bất đồng bộ qua Hangfire worker, tự động thử lại tối đa 3 lần khi xảy ra lỗi.
/// </summary>
public class ImageResizeJob : IImageResizeJob
{
    private const int MediumMaxWidth = 800;
    private const int ThumbnailMaxWidth = 300;
    private const string DefaultBucket = "culinaryblog";

    private readonly IApplicationDbContext _dbContext;
    private readonly IStorageService _storageService;
    private readonly ILogger<ImageResizeJob> _logger;
    private readonly HttpClient _httpClient;

    public ImageResizeJob(
        IApplicationDbContext dbContext,
        IStorageService storageService,
        ILogger<ImageResizeJob> logger,
        HttpClient? httpClient = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Xử lý tải ảnh gốc, decode, resize tạo Medium và Thumbnail, tải lên MinIO và cập nhật Database.
    /// Hangfire cấu hình AutomaticRetry tối đa 3 lần theo yêu cầu Task 4.
    /// </summary>
    /// <param name="recipeImageId">Định danh bản ghi RecipeImage.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ProcessAsync(Guid recipeImageId, CancellationToken ct = default)
    {
        _logger.LogInformation("Bắt đầu xử lý ImageResizeJob cho RecipeImage {RecipeImageId}.", recipeImageId);

        // 1. Tải RecipeImage từ cơ sở dữ liệu
        var image = await _dbContext.RecipeImages
            .FirstOrDefaultAsync(x => x.Id == recipeImageId, ct);

        if (image == null)
        {
            _logger.LogWarning("Không tìm thấy RecipeImage {RecipeImageId} trong cơ sở dữ liệu. Bỏ qua job.", recipeImageId);
            return;
        }

        // 2. Kiểm tra nếu ảnh đã bị đánh dấu xóa mềm
        if (image.IsDeleted)
        {
            _logger.LogWarning("RecipeImage {RecipeImageId} đã bị xóa mềm (IsDeleted = true). Bỏ qua job.", recipeImageId);
            return;
        }

        // 3. Tính lũy đẳng (Idempotency): Nếu ảnh đã có cả MediumUrl và ThumbnailUrl thì không cần làm lại
        if (!string.IsNullOrWhiteSpace(image.MediumUrl) && !string.IsNullOrWhiteSpace(image.ThumbnailUrl))
        {
            _logger.LogInformation("RecipeImage {RecipeImageId} đã có đầy đủ MediumUrl và ThumbnailUrl. Bỏ qua để đảm bảo tính Idempotent.", recipeImageId);
            return;
        }

        if (string.IsNullOrWhiteSpace(image.OriginalUrl))
        {
            _logger.LogError("RecipeImage {RecipeImageId} không có OriginalUrl hợp lệ.", recipeImageId);
            throw new InvalidOperationException($"RecipeImage {recipeImageId} không có OriginalUrl để xử lý.");
        }

        // 4. Lấy luồng dữ liệu ảnh gốc từ MinIO hoặc URL bên ngoài
        using var originalStream = await GetOriginalImageStreamAsync(image.OriginalUrl, ct);

        // 5. Decode ảnh thật bằng SixLabors.ImageSharp
        using var imageSharp = await Image.LoadAsync(originalStream, ct);
        var detectedFormat = imageSharp.Metadata.DecodedImageFormat;
        if (detectedFormat == null)
        {
            _logger.LogError("Không thể nhận diện định dạng ảnh của RecipeImage {RecipeImageId}.", recipeImageId);
            throw new InvalidOperationException($"Không thể giải mã định dạng ảnh cho RecipeImage {recipeImageId}.");
        }

        _logger.LogInformation(
            "Đã decode thành công ảnh RecipeImage {RecipeImageId}. Kích thước gốc: {Width}x{Height}, Định dạng: {Format}.",
            recipeImageId,
            imageSharp.Width,
            imageSharp.Height,
            detectedFormat.Name);

        // 6. Tạo ảnh Medium (tối đa chiều rộng 800px, giữ nguyên tỷ lệ khung hình aspect ratio)
        var mediumOptions = new ResizeOptions
        {
            Size = new Size(MediumMaxWidth, 0),
            Mode = ResizeMode.Max,
        };
        using var mediumImage = imageSharp.Clone(ctx => ctx.Resize(mediumOptions));

        // 7. Tạo ảnh Thumbnail (tối đa chiều rộng 300px, giữ nguyên tỷ lệ khung hình aspect ratio)
        var thumbOptions = new ResizeOptions
        {
            Size = new Size(ThumbnailMaxWidth, 0),
            Mode = ResizeMode.Max,
        };
        using var thumbImage = imageSharp.Clone(ctx => ctx.Resize(thumbOptions));

        // 8. Encode sang Stream tương ứng với định dạng gốc
        using var mediumMs = new MemoryStream();
        await mediumImage.SaveAsync(mediumMs, detectedFormat, ct);
        mediumMs.Position = 0;

        using var thumbMs = new MemoryStream();
        await thumbImage.SaveAsync(thumbMs, detectedFormat, ct);
        thumbMs.Position = 0;

        // 9. Sinh Object Key và tải lên Object Storage (MinIO)
        var contentType = detectedFormat.DefaultMimeType;
        var ext = detectedFormat.FileExtensions.FirstOrDefault() ?? "jpg";
        if (!ext.StartsWith('.'))
        {
            ext = $".{ext}";
        }

        var mediumKey = $"recipes/{image.RecipeId}/images/{image.Id}_medium{ext}";
        var thumbKey = $"recipes/{image.RecipeId}/images/{image.Id}_thumb{ext}";

        var mediumUrl = await _storageService.UploadAsync(DefaultBucket, mediumKey, mediumMs, contentType, ct);
        var thumbUrl = await _storageService.UploadAsync(DefaultBucket, thumbKey, thumbMs, contentType, ct);

        // 10. Cập nhật Database chỉ khi đã upload thành công cả 2 ảnh
        image.MediumUrl = mediumUrl;
        image.ThumbnailUrl = thumbUrl;
        image.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Hoàn thành tạo Medium và Thumbnail cho RecipeImage {RecipeImageId}: Medium={MediumUrl}, Thumbnail={ThumbnailUrl}.",
            recipeImageId,
            mediumUrl,
            thumbUrl);
    }

    private async Task<Stream> GetOriginalImageStreamAsync(string originalUrl, CancellationToken ct)
    {
        // 1. Thử lấy từ MinIO nếu là key nội bộ hoặc URL MinIO
        if (TryExtractBucketAndKey(originalUrl, out var bucket, out var key))
        {
            try
            {
                return await _storageService.DownloadAsync(bucket, key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tải ảnh qua IStorageService cho key '{Key}'. Sẽ thử tải qua HTTP.", key);
            }
        }

        // 2. Thử tải qua HTTP/HTTPS nếu là URL từ Internet (seed data hoặc CDN)
        if (Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var response = await _httpClient.GetAsync(uri, ct);
            response.EnsureSuccessStatusCode();

            var ms = new MemoryStream();
            await response.Content.CopyToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }

        throw new InvalidOperationException($"Không thể xác định nguồn dữ liệu để tải ảnh từ OriginalUrl: '{originalUrl}'.");
    }

    private static bool TryExtractBucketAndKey(string url, out string bucket, out string key)
    {
        bucket = DefaultBucket;
        key = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();

        // Trường hợp 1: Đường dẫn tương đối dạng "recipes/{recipeId}/images/..."
        if (trimmed.StartsWith("recipes/", StringComparison.OrdinalIgnoreCase))
        {
            key = trimmed;
            return true;
        }

        // Trường hợp 2: URL đầy đủ dạng "http://localhost:9000/culinaryblog/recipes/..."
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath.TrimStart('/');
            var firstSlash = path.IndexOf('/');
            if (firstSlash > 0)
            {
                bucket = path[..firstSlash];
                key = path[(firstSlash + 1)..];
                return true;
            }
        }

        return false;
    }
}
