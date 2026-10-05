using System.Text;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Application.Exceptions;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Common;

/// <summary>
/// Unit tests cho ImageValidator:
/// - Kiểm tra MIME type, Magic bytes (chữ ký số tệp) cho JPEG, PNG, WebP, AVIF.
/// - Kiểm tra giới hạn dung lượng tệp tối đa 5 MB.
/// - Kiểm tra phát hiện và ngăn chặn giả mạo định dạng (disguised files: exe, html, txt).
/// </summary>
public class RecipeImageValidationTests
{
    // Mẫu Header JPEG hợp lệ: SOI (FF D8) + APP0 marker (FF E0) + JFIF identifier
    private static readonly byte[] ValidJpegBytes =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46,
        0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
        0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43
    ];

    // Mẫu Header PNG hợp lệ: 8-byte signature + IHDR chunk
    private static readonly byte[] ValidPngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x5C, 0x72, 0xA8
    ];

    // Mẫu Header WebP hợp lệ: RIFF .... WEBP + VP8 chunk
    private static readonly byte[] ValidWebpBytes =
    [
        0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00,
        0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20,
        0x18, 0x00, 0x00, 0x00, 0x30, 0x01, 0x00, 0x9D
    ];

    // Mẫu Header AVIF hợp lệ: ftyp box với major brand avif
    private static readonly byte[] ValidAvifBytes =
    [
        0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70,
        0x61, 0x76, 0x69, 0x66, 0x00, 0x00, 0x00, 0x00,
        0x61, 0x76, 0x69, 0x66, 0x6D, 0x69, 0x66, 0x31
    ];

    // Mẫu Header PE Windows Executable (virus.exe / application)
    private static readonly byte[] FakeExeBytes =
    [
        0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
        0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00,
        0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    [Fact]
    public void Validate_ValidJpeg_ReturnsSuccess()
    {
        using var stream = new MemoryStream(ValidJpegBytes);

        var result = ImageValidator.Validate(stream, "image/jpeg", "test.jpg", ValidJpegBytes.Length);

        result.IsValid.Should().BeTrue();
        result.DetectedMimeType.Should().Be("image/jpeg");
        result.Extension.Should().Be(".jpg");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Validate_ValidPng_ReturnsSuccess()
    {
        using var stream = new MemoryStream(ValidPngBytes);

        var result = ImageValidator.Validate(stream, "image/png", "photo.png", ValidPngBytes.Length);

        result.IsValid.Should().BeTrue();
        result.DetectedMimeType.Should().Be("image/png");
        result.Extension.Should().Be(".png");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Validate_ValidWebp_ReturnsSuccess()
    {
        using var stream = new MemoryStream(ValidWebpBytes);

        var result = ImageValidator.Validate(stream, "image/webp", "image.webp", ValidWebpBytes.Length);

        result.IsValid.Should().BeTrue();
        result.DetectedMimeType.Should().Be("image/webp");
        result.Extension.Should().Be(".webp");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Validate_ValidAvif_ReturnsSuccess()
    {
        using var stream = new MemoryStream(ValidAvifBytes);

        var result = ImageValidator.Validate(stream, "image/avif", "banner.avif", ValidAvifBytes.Length);

        result.IsValid.Should().BeTrue();
        result.DetectedMimeType.Should().Be("image/avif");
        result.Extension.Should().Be(".avif");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Validate_FileSizeExceeds5Mb_ReturnsError()
    {
        using var stream = new MemoryStream(ValidJpegBytes);
        long oversized = (5 * 1024 * 1024) + 1; // 5 MB + 1 byte

        var result = ImageValidator.Validate(stream, "image/jpeg", "huge.jpg", oversized);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("5 MB");
    }

    [Fact]
    public void Validate_EmptyFile_ReturnsError()
    {
        using var stream = new MemoryStream();

        var result = ImageValidator.Validate(stream, "image/jpeg", "empty.jpg", 0);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("trống");
    }

    [Fact]
    public void Validate_HeaderTooShort_ReturnsError()
    {
        var shortBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        using var stream = new MemoryStream(shortBytes);

        var result = ImageValidator.Validate(stream, "image/jpeg", "short.jpg", shortBytes.Length);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không đủ kích thước");
    }

    [Fact]
    public void Validate_UnsupportedMimeType_ReturnsError()
    {
        using var stream = new MemoryStream(ValidJpegBytes);

        var result = ImageValidator.Validate(stream, "application/pdf", "doc.pdf", ValidJpegBytes.Length);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không được hỗ trợ");
    }

    [Fact]
    public void Validate_MimeMismatch_DeclaredJpeg_ActualPng_ReturnsError()
    {
        // Client khai báo image/jpeg nhưng gửi nội dung file PNG
        using var stream = new MemoryStream(ValidPngBytes);

        var result = ImageValidator.Validate(stream, "image/jpeg", "fake.jpg", ValidPngBytes.Length);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không khớp");
    }

    [Fact]
    public void Validate_DisguisedExeFile_RenamedToJpg_ReturnsError()
    {
        // Kịch bản tấn công: Đổi tên virus.exe -> avatar.jpg và khai báo MIME image/jpeg
        using var stream = new MemoryStream(FakeExeBytes);

        var result = ImageValidator.Validate(stream, "image/jpeg", "avatar.jpg", FakeExeBytes.Length);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("magic bytes");
    }

    [Fact]
    public void Validate_DisguisedHtmlFile_RenamedToPng_ReturnsError()
    {
        // Kịch bản tấn công: File text HTML đổi tên thành anh.png
        var htmlBytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body><script>alert('XSS')</script></body></html>");
        using var stream = new MemoryStream(htmlBytes);

        var result = ImageValidator.Validate(stream, "image/png", "anh.png", htmlBytes.Length);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("magic bytes");
    }

    [Fact]
    public void EnsureValid_ThrowsValidationException_WhenFileInvalid()
    {
        using var stream = new MemoryStream(FakeExeBytes);

        var action = () => ImageValidator.EnsureValid(stream, "image/jpeg", "avatar.jpg", FakeExeBytes.Length);

        action.Should().Throw<ValidationException>()
            .Which.Errors.Should().ContainKey("File");
    }

    [Fact]
    public void EnsureValid_DoesNotThrow_WhenFileValid()
    {
        using var stream = new MemoryStream(ValidJpegBytes);

        var action = () => ImageValidator.EnsureValid(stream, "image/jpeg", "valid.jpg", ValidJpegBytes.Length);

        action.Should().NotThrow();
    }
}

