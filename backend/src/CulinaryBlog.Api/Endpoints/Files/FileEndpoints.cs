using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Api.Endpoints.Files;

public static class FileEndpoints
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/avif"];

    public static void MapFileEndpoints(this WebApplication app)
    {
        app.MapPost("/api/files/images", UploadImages).DisableAntiforgery().RequireAuthorization().WithTags("Files");
        app.MapPost("/api/v1/files/images", UploadImages).DisableAntiforgery().RequireAuthorization().WithTags("Files");
    }

    private static async Task<IResult> UploadImages(IFormFileCollection files, HttpRequest request, IMinioClient minio, IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return Results.BadRequest(new { error = "Chưa chọn ảnh để tải lên." });
        }

        var bucketName = configuration["Minio:BucketName"] ?? "culinary-blog";
        var publicEndpoint = (configuration["Minio:PublicEndpoint"] ?? configuration["Minio:Endpoint"] ?? $"{request.Scheme}://{request.Host}").TrimEnd('/');
        var bucketExists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), cancellationToken);
        if (!bucketExists)
        {
            await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), cancellationToken);
        }

        await minio.SetPolicyAsync(new SetPolicyArgs().WithBucket(bucketName).WithPolicy("public"), cancellationToken);

        var uploaded = new List<ImageUploadResponse>();

        foreach (var file in files)
        {
            if (file.Length == 0 || file.Length > MaxFileSize || !AllowedContentTypes.Contains(file.ContentType) || !await HasValidSignatureAsync(file, cancellationToken))
            {
                return Results.BadRequest(new { error = "Chỉ chấp nhận ảnh JPEG, PNG, WebP hoặc AVIF tối đa 5 MB mỗi file." });
            }

            var extension = file.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/avif" => ".avif",
                _ => string.Empty
            };
            var objectName = $"uploads/{Guid.NewGuid():N}{extension}";
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;
            await minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType), cancellationToken);
            uploaded.Add(new ImageUploadResponse($"{publicEndpoint}/{bucketName}/{objectName}", file.FileName));
        }

        return Results.Ok(uploaded);
    }

    private static async Task<bool> HasValidSignatureAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        return file.ContentType switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "image/avif" => read >= 12 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            _ => false
        };
    }
}

public sealed record ImageUploadResponse(string Url, string OriginalName);
