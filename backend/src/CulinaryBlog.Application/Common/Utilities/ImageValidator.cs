using System;
using System.IO;
using System.Collections.Generic;
using CulinaryBlog.Application.Exceptions;
using HeyRed.ImageSharp.Heif.Formats.Avif;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CulinaryBlog.Application.Common.Utilities;

public record ImageValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string DetectedMimeType,
    string Extension);

/// <summary>
/// Trình kiểm tra an toàn và hợp lệ cho tệp hình ảnh:
/// - Giới hạn kích thước (tối đa 5 MB).
/// - Định dạng cho phép: JPEG, PNG, WebP, AVIF.
/// - Kiểm tra MIME type, Magic bytes (chữ ký số tệp ISO BMFF cho AVIF, header signature cho JPEG/PNG/WebP).
/// - Giải mã thực tế (decode) toàn bộ cấu trúc hình ảnh nhằm ngăn chặn payload giả mạo / file hỏng.
/// </summary>
public static class ImageValidator
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/avif"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".avif"
    };

    static ImageValidator()
    {
        Configuration.Default.Configure(new AvifConfigurationModule());
    }

    public static ImageValidationResult Validate(
        Stream stream,
        string? declaredContentType,
        string? fileName,
        long fileLength)
    {
        // 1. Kiểm tra kích thước tệp
        if (fileLength <= 0)
        {
            return new ImageValidationResult(false, "Tệp tải lên không được để trống (File is empty).", string.Empty, string.Empty);
        }

        if (fileLength > MaxFileSizeBytes)
        {
            return new ImageValidationResult(
                false,
                $"Kích thước tệp ({fileLength} bytes) vượt quá giới hạn tối đa cho phép là {MaxFileSizeBytes} bytes (5 MB).",
                string.Empty,
                string.Empty);
        }

        // 2. Kiểm tra Content-Type từ client
        var normalizedMime = NormalizeMimeType(declaredContentType);
        if (string.IsNullOrWhiteSpace(normalizedMime) || !AllowedMimeTypes.Contains(normalizedMime))
        {
            return new ImageValidationResult(
                false,
                $"Định dạng MIME '{declaredContentType}' không được hỗ trợ. Hệ thống chỉ chấp nhận: JPEG, PNG, WebP, AVIF.",
                string.Empty,
                string.Empty);
        }

        // 3. Đọc Magic Bytes từ Stream (tối đa 64 bytes đầu)
        byte[] header = new byte[64];
        int bytesRead;

        long originalPosition = 0;
        bool canSeek = stream.CanSeek;
        if (canSeek)
        {
            originalPosition = stream.Position;
        }

        try
        {
            bytesRead = stream.Read(header, 0, header.Length);
        }
        finally
        {
            if (canSeek)
            {
                stream.Position = originalPosition;
            }
        }

        if (bytesRead < 12)
        {
            return new ImageValidationResult(
                false,
                "Tệp không đủ kích thước để xác thực định dạng hình ảnh hợp lệ.",
                string.Empty,
                string.Empty);
        }

        // 4. Phát hiện định dạng thực tế qua Magic Bytes / Header Signature
        var (detectedMime, extension) = DetectFormatFromSignature(header, bytesRead);

        if (string.IsNullOrEmpty(detectedMime))
        {
            return new ImageValidationResult(
                false,
                "Nội dung tệp (magic bytes) không khớp với bất kỳ định dạng ảnh hợp lệ nào (JPEG, PNG, WebP, AVIF). Tệp có thể đã bị làm giả hoặc bị hỏng.",
                string.Empty,
                string.Empty);
        }

        // 5. Kiểm tra tính nhất quán giữa MIME khai báo và nội dung thực tế
        var expectedNormalized = normalizedMime == "image/jpg" ? "image/jpeg" : normalizedMime;
        if (!string.Equals(expectedNormalized, detectedMime, StringComparison.OrdinalIgnoreCase))
        {
            return new ImageValidationResult(
                false,
                $"Định dạng tệp không khớp: Khách hàng khai báo '{declaredContentType}' nhưng nội dung thực tế là '{detectedMime}'.",
                string.Empty,
                string.Empty);
        }

        // 6. Decode nội dung ảnh thực tế để đảm bảo file không bị hỏng hoặc chứa payload giả mạo
        try
        {
            if (canSeek)
            {
                stream.Position = originalPosition;
            }

            using var image = Image.Load<Rgba32>(stream);
            if (image.Width <= 0 || image.Height <= 0)
            {
                return new ImageValidationResult(
                    false,
                    "Tệp hình ảnh có kích thước không hợp lệ (Width/Height <= 0).",
                    string.Empty,
                    string.Empty);
            }
        }
        catch (Exception)
        {
            return new ImageValidationResult(
                false,
                "Dữ liệu hình ảnh bị hỏng hoặc không thể giải mã (Corrupted or invalid image payload).",
                string.Empty,
                string.Empty);
        }
        finally
        {
            if (canSeek)
            {
                stream.Position = originalPosition;
            }
        }

        return new ImageValidationResult(true, null, detectedMime, extension);
    }

    public static void EnsureValid(Stream stream, string? declaredContentType, string? fileName, long fileLength)
    {
        var result = Validate(stream, declaredContentType, fileName, fileLength);
        if (!result.IsValid)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["File"] = [result.ErrorMessage ?? "Tệp hình ảnh không hợp lệ."]
            });
        }
    }

    private static (string MimeType, string Extension) DetectFormatFromSignature(byte[] header, int length)
    {
        // JPEG: FF D8 FF
        if (length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return ("image/jpeg", ".jpg");
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (length >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 && // P
            header[2] == 0x4E && // N
            header[3] == 0x47 && // G
            header[4] == 0x0D && // \r
            header[5] == 0x0A && // \n
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            return ("image/png", ".png");
        }

        // WebP: RIFF .... WEBP
        // Offset 0..3: RIFF (0x52, 0x49, 0x46, 0x46)
        // Offset 8..11: WEBP (0x57, 0x45, 0x42, 0x50)
        if (length >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return ("image/webp", ".webp");
        }

        // AVIF: ISO BMFF box with 'ftyp' (offset 4..7) and major or compatible brand 'avif' or 'avis'
        if (length >= 12 &&
            header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70) // ftyp
        {
            // Kiểm tra major brand tại offset 8..11
            bool isAvifBrand = (header[8] == 0x61 && header[9] == 0x76 && header[10] == 0x69 && header[11] == 0x66) || // avif
                               (header[8] == 0x61 && header[9] == 0x76 && header[10] == 0x69 && header[11] == 0x73);   // avis

            if (isAvifBrand)
            {
                return ("image/avif", ".avif");
            }

            // Kiểm tra compatible brands tại offset 16 trở đi
            for (int i = 16; i + 4 <= length; i += 4)
            {
                if ((header[i] == 0x61 && header[i + 1] == 0x76 && header[i + 2] == 0x69 && header[i + 3] == 0x66) ||
                    (header[i] == 0x61 && header[i + 1] == 0x76 && header[i + 2] == 0x69 && header[i + 3] == 0x73))
                {
                    return ("image/avif", ".avif");
                }
            }
        }

        return (string.Empty, string.Empty);
    }

    private static string NormalizeMimeType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        var parts = contentType.Split(';');
        return parts[0].Trim().ToLowerInvariant();
    }
}
