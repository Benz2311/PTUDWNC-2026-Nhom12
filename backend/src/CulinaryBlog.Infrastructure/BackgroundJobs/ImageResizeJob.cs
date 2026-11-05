using CulinaryBlog.Application.Interfaces;
using HeyRed.ImageSharp.Heif.Formats.Avif;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

/// <summary>
/// Background Job chạy ngầm thông qua Hangfire để tự động resize ảnh công thức nấu ăn:
/// - Tạo ảnh Medium (chiều rộng tối đa 800px).
/// - Tạo ảnh Thumbnail (chiều rộng tối đa 300px).
/// - Đọc JPEG, PNG, WebP, AVIF; lưu các ảnh phái sinh dưới định dạng WebP.
/// - Chèn watermark CulinaryBlog vào ảnh Medium và Thumbnail.
/// - Lưu trữ tệp sau resize lên Object Storage (MinIO).
/// - Cập nhật trực tiếp URL vào RecipeImage trong CSDL.
/// - Đảm bảo tính lũy đẳng (Idempotency): nếu ảnh đã xử lý thì bỏ qua.
/// </summary>
public class ImageResizeJob : IImageResizeJob
{
    private const int MediumMaxWidth = 800;
    private const int ThumbnailMaxWidth = 300;
    private const string DefaultBucket = "culinaryblog";
    private const string WatermarkText = "CULINARYBLOG";
    private static readonly IReadOnlyDictionary<char, string[]> WatermarkGlyphs = new Dictionary<char, string[]>
    {
        ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['B'] = ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
        ['C'] = ["01110", "10001", "10000", "10000", "10000", "10001", "01110"],
        ['G'] = ["01110", "10001", "10000", "10111", "10001", "10001", "01110"],
        ['I'] = ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
        ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        ['N'] = ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"]
    };

    private readonly IApplicationDbContext _dbContext;
    private readonly IStorageService _storageService;
    private readonly ILogger<ImageResizeJob> _logger;
    private readonly HttpClient _httpClient;

    static ImageResizeJob()
    {
        Configuration.Default.Configure(new AvifConfigurationModule());
    }

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

        // 5. Decode ảnh thật bằng SixLabors.ImageSharp (kèm AVIF decoder module)
        using var imageSharp = await Image.LoadAsync<Rgba32>(originalStream, ct);
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
        AddWatermark(mediumImage);

        // 7. Tạo ảnh Thumbnail (tối đa chiều rộng 300px, giữ nguyên tỷ lệ khung hình aspect ratio)
        var thumbOptions = new ResizeOptions
        {
            Size = new Size(ThumbnailMaxWidth, 0),
            Mode = ResizeMode.Max,
        };
        using var thumbImage = imageSharp.Clone(ctx => ctx.Resize(thumbOptions));
        AddWatermark(thumbImage);

        const string outputMime = "image/webp";
        const string ext = ".webp";
        var encoder = new WebpEncoder { Quality = 82 };

        using var mediumMs = new MemoryStream();
        await mediumImage.SaveAsync(mediumMs, encoder, ct);
        mediumMs.Position = 0;

        using var thumbMs = new MemoryStream();
        await thumbImage.SaveAsync(thumbMs, encoder, ct);
        thumbMs.Position = 0;

        // 9. Sinh Object Key và tải lên Object Storage (MinIO)
        var mediumKey = $"recipes/{image.RecipeId}/images/{image.Id}_medium{ext}";
        var thumbKey = $"recipes/{image.RecipeId}/images/{image.Id}_thumb{ext}";

        var mediumUrl = await _storageService.UploadAsync(DefaultBucket, mediumKey, mediumMs, outputMime, ct);
        var thumbUrl = await _storageService.UploadAsync(DefaultBucket, thumbKey, thumbMs, outputMime, ct);

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

    private static void AddWatermark(Image<Rgba32> image)
    {
        var scale = Math.Clamp(image.Width / 250, 1, 4);
        var padding = scale * 2;
        var textWidth = (WatermarkText.Length * 6 - 1) * scale;
        var textHeight = 7 * scale;
        var blockWidth = textWidth + (padding * 2);
        var blockHeight = textHeight + (padding * 2);
        var left = Math.Max(0, image.Width - blockWidth - scale * 2);
        var top = Math.Max(0, image.Height - blockHeight - scale * 2);

        image.ProcessPixelRows(accessor =>
        {
            for (var y = top; y < Math.Min(top + blockHeight, image.Height); y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = left; x < Math.Min(left + blockWidth, image.Width); x++)
                    row[x] = Blend(row[x], new Rgba32(12, 24, 20, 165));
            }

            var textLeft = left + padding;
            var textTop = top + padding;
            for (var index = 0; index < WatermarkText.Length; index++)
            {
                var glyph = WatermarkGlyphs[WatermarkText[index]];
                var glyphLeft = textLeft + index * 6 * scale;
                for (var glyphY = 0; glyphY < glyph.Length; glyphY++)
                {
                    for (var glyphX = 0; glyphX < glyph[glyphY].Length; glyphX++)
                    {
                        if (glyph[glyphY][glyphX] != '1')
                            continue;

                        for (var yOffset = 0; yOffset < scale; yOffset++)
                        {
                            var y = textTop + glyphY * scale + yOffset;
                            if (y >= image.Height)
                                continue;

                            var row = accessor.GetRowSpan(y);
                            for (var xOffset = 0; xOffset < scale; xOffset++)
                            {
                                var x = glyphLeft + glyphX * scale + xOffset;
                                if (x < image.Width)
                                    row[x] = Blend(row[x], new Rgba32(255, 255, 255, 235));
                            }
                        }
                    }
                }
            }
        });
    }

    private static Rgba32 Blend(Rgba32 background, Rgba32 overlay)
    {
        var alpha = overlay.A / 255f;
        return new Rgba32(
            (byte)(background.R * (1 - alpha) + overlay.R * alpha),
            (byte)(background.G * (1 - alpha) + overlay.G * alpha),
            (byte)(background.B * (1 - alpha) + overlay.B * alpha),
            255);
    }
}
