using System;
using System.IO;
using CulinaryBlog.Application.Common.Utilities;
using FluentAssertions;
using HeyRed.ImageSharp.Heif.Formats.Avif;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Common;

/// <summary>
/// Các bài kiểm thử chính thức cho định dạng AVIF:
/// - Chứng minh Image.Load giải mã (decode) thành công AVIF nhị phân thật (width > 0, height > 0).
/// - Xác nhận ImageValidator chấp nhận AVIF hợp lệ.
/// - Xác nhận từ chối file AVIF hỏng (corrupt payload).
/// - Xác nhận từ chối file MIME image/avif nhưng payload giả mạo (disguised/fake).
/// - Xác nhận từ chối file AVIF vượt quá 5 MB.
/// </summary>
public class ImageValidatorAvifTests
{
    // Chuỗi nhị phân AVIF 1x1 thật hợp lệ được mã hóa Base64 chuẩn
    private static readonly byte[] ValidAvifFileBytes = Convert.FromBase64String(
        "AAAAIGZ0eXBhdmlmAAAAAGF2aWZtaWYxbWlhZk1BMUIAAADybWV0YQAAAAAAAAAoaGRscgAAAAAAAAAAcGljdAAAAAAAAAAAAAAAAGxpYmF2aWYAAAAADnBpdG0AAAAAAAEAAAAeaWxvYwAAAABEAAABAAEAAAABAAABGgAAAB0AAAAoaWluZgAAAAAAAQAAABppbmZlAgAAAAABAABhdjAxQ29sb3IAAAAAamlwcnAAAABLaXBjbwAAABRpc3BlAAAAAAAAAAIAAAACAAAAEHBpeGkAAAAAAwgICAAAAAxhdjFDgQ0MAAAAABNjb2xybmNseAACAAIAAYAAAAAXaXBtYQAAAAAAAAABAAEEAQKDBAAAACVtZGF0EgAKCBgANogQEAwgMg8f8D///8WfhwB8+ErK42A=");

    // File giả mạo: Đúng ISO BMFF ftyp box với brand 'avif', nhưng payload phía sau hoàn toàn hỏng
    private static readonly byte[] FakeAvifPayloadBytes =
    [
        0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70,
        0x61, 0x76, 0x69, 0x66, 0x00, 0x00, 0x00, 0x00,
        0x61, 0x76, 0x69, 0x66, 0x6D, 0x69, 0x66, 0x31,
        0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00, 0x11
    ];

    [Fact]
    public void AvifActualDecode_ReturnsPositiveDimensions()
    {
        // Yêu cầu mục 4: Chứng minh Image.Load decode được AVIF thật, width > 0, height > 0
        Configuration.Default.Configure(new AvifConfigurationModule());

        using var stream = new MemoryStream(ValidAvifFileBytes);
        using var image = Image.Load<Rgba32>(stream);

        image.Should().NotBeNull();
        image.Width.Should().BeGreaterThan(0, "AVIF decode phải cho ra chiều rộng dương");
        image.Height.Should().BeGreaterThan(0, "AVIF decode phải cho ra chiều cao dương");
        image.Metadata.DecodedImageFormat.Should().NotBeNull();
        image.Metadata.DecodedImageFormat!.Name.Should().Be("AVIF");
    }

    [Fact]
    public void ValidAvif_IsAccepted()
    {
        // Yêu cầu mục 3: ValidAvif_IsAccepted
        using var stream = new MemoryStream(ValidAvifFileBytes);

        var result = ImageValidator.Validate(stream, "image/avif", "dish.avif", ValidAvifFileBytes.Length);

        result.IsValid.Should().BeTrue();
        result.DetectedMimeType.Should().Be("image/avif");
        result.Extension.Should().Be(".avif");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void InvalidAvif_IsRejected()
    {
        // Yêu cầu mục 3: InvalidAvif_IsRejected (file hỏng, không đủ header / corrupt)
        var corruptBytes = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x66, 0x74, 0x79, 0x70, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(corruptBytes);

        var result = ImageValidator.Validate(stream, "image/avif", "corrupt.avif", corruptBytes.Length);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AvifMimeWithFakePayload_IsRejected()
    {
        // Yêu cầu mục 3: AvifMimeWithFakePayload_IsRejected
        // MIME là image/avif, magic bytes là ftyp avif nhưng payload không thể decode
        using var stream = new MemoryStream(FakeAvifPayloadBytes);

        var result = ImageValidator.Validate(stream, "image/avif", "fake.avif", FakeAvifPayloadBytes.Length);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không thể giải mã");
    }

    [Fact]
    public void AvifOver5Mb_IsRejected()
    {
        // Yêu cầu mục 3: AvifOver5Mb_IsRejected
        using var stream = new MemoryStream(ValidAvifFileBytes);
        long oversized = (5 * 1024 * 1024) + 1;

        var result = ImageValidator.Validate(stream, "image/avif", "huge.avif", oversized);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("5 MB");
    }
}
