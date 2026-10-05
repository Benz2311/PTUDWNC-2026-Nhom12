namespace CulinaryBlog.Application.Common.Utilities;

/// <summary>
/// Tiện ích chuẩn hóa và sinh Object Key an toàn cho Object Storage (MinIO / S3).
/// Đảm bảo tính duy nhất (Unique), loại bỏ nguy cơ Path Traversal và không tin cậy filename từ client.
/// </summary>
public static class StoragePathHelper
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    /// <summary>
    /// Kiểm tra và làm sạch Object Key.
    /// Chống Path Traversal (chứa "..", bắt đầu bằng "/", chứa "\", ký tự điều khiển hoặc ký tự ổ đĩa).
    /// </summary>
    /// <param name="key">Đường dẫn object key cần kiểm tra.</param>
    /// <returns>Object key đã được chuẩn hóa.</returns>
    /// <exception cref="ArgumentException">Ném ra nếu key rỗng hoặc có dấu hiệu Path Traversal nguy hiểm.</exception>
    public static string SanitizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Object key không được để trống.", nameof(key));
        }

        var normalized = key.Trim();

        // Kiểm tra ký tự null hoặc ký tự điều khiển
        if (normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Object key chứa ký tự điều khiển không hợp lệ.", nameof(key));
        }

        // Chống drive letter (ví dụ C:\)
        if (normalized.Contains(':'))
        {
            throw new ArgumentException("Object key không được chứa ký tự ':' (drive letter).", nameof(key));
        }

        // Thay thế dấu gạch chéo ngược Windows thành gạch chéo xuôi chuẩn S3
        normalized = normalized.Replace('\\', '/');

        // Không cho phép đường dẫn bắt đầu bằng '/'
        if (normalized.StartsWith('/'))
        {
            throw new ArgumentException($"Object key phát hiện nguy cơ Path Traversal (đường dẫn bắt đầu bằng '/'): {key}", nameof(key));
        }

        // Chống Path Traversal ("..")
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment == ".." || segment == ".")
            {
                throw new ArgumentException($"Object key phát hiện dấu hiệu Path Traversal nguy hiểm ('{segment}'): {key}", nameof(key));
            }
        }

        if (normalized.Contains(".."))
        {
            throw new ArgumentException($"Object key chứa chuỗi '..' không hợp lệ: {key}", nameof(key));
        }

        return normalized;
    }

    /// <summary>
    /// Chuẩn hóa phần mở rộng (extension) từ tên file của client.
    /// Tuyệt đối không dùng tên file gốc của client để tạo path nhằm tránh tấn công path traversal và giả mạo file.
    /// </summary>
    /// <param name="fileName">Tên file do client gửi lên (chỉ trích xuất extension).</param>
    /// <param name="defaultExtension">Extension mặc định nếu không xác định được.</param>
    /// <returns>Extension hợp lệ (ví dụ: .jpg, .png).</returns>
    public static string SanitizeExtension(string? fileName, string defaultExtension = ".jpg")
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return defaultExtension;
        }

        try
        {
            var ext = Path.GetExtension(fileName.Trim()).ToLowerInvariant();
            if (AllowedImageExtensions.Contains(ext))
            {
                return ext;
            }
        }
        catch
        {
            // Bỏ qua lỗi parse filename từ client
        }

        return defaultExtension;
    }

    /// <summary>
    /// Sinh Object Key an toàn, duy nhất cho ảnh bài viết (Recipe Image) theo chuẩn:
    /// recipes/{recipeId}/images/{generated-guid}{extension}
    /// </summary>
    /// <param name="recipeId">ID bài viết (Recipe).</param>
    /// <param name="extension">Phần mở rộng file (ví dụ .jpg).</param>
    /// <returns>Object key an toàn do máy chủ kiểm soát hoàn toàn.</returns>
    public static string GenerateRecipeImageKey(Guid recipeId, string extension)
    {
        var sanitizedExt = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        if (!AllowedImageExtensions.Contains(sanitizedExt))
        {
            sanitizedExt = ".jpg";
        }

        var uniqueId = Guid.NewGuid().ToString("N");
        return $"recipes/{recipeId}/images/{uniqueId}{sanitizedExt}";
    }

    /// <summary>
    /// Sinh Object Key chung có tiền tố (prefix) và định danh duy nhất (Guid).
    /// </summary>
    /// <param name="folderPrefix">Thư mục tiền tố (ví dụ: avatars, recipes/{id}).</param>
    /// <param name="extension">Phần mở rộng file.</param>
    /// <returns>Object key an toàn.</returns>
    public static string GenerateUniqueKey(string folderPrefix, string extension)
    {
        var sanitizedPrefix = SanitizeKey(folderPrefix.Trim().Trim('/'));
        var sanitizedExt = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        var uniqueId = Guid.NewGuid().ToString("N");
        return $"{sanitizedPrefix}/{uniqueId}{sanitizedExt}";
    }
}
