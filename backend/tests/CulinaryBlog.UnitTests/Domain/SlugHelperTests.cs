using CulinaryBlog.Domain.Common;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.UnitTests.Domain;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Spaces  ", "spaces")]
    [InlineData("Phở Bò", "pho-bo")]
    [InlineData("Bún Bò Huế", "bun-bo-hue")]
    [InlineData("Cơm Tấm Sườn", "com-tam-suon")]
    [InlineData("Đặc Sản Miền Tây", "dac-san-mien-tay")]
    [InlineData("already-slug", "already-slug")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    public void Generate_WithVariousInputs_ReturnsExpectedSlug(string input, string expected)
    {
        // Act
        var result = SlugHelper.Generate(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Generate_WithEmptyOrWhitespaceInput_ReturnsEmptyString(string? input)
    {
        // Act
        var result = SlugHelper.Generate(input!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithSpecialCharacters_RemovesNonAlphanumeric()
    {
        // Act
        var result = SlugHelper.Generate("Hello! @World# $2024%");

        // Assert
        result.Should().Be("hello-world-2024");
    }

    [Fact]
    public void Generate_WithDiacritics_ProducesAsciiSlug()
    {
        // Act
        var result = SlugHelper.Generate("Ánh Nhung");

        // Assert
        result.Should().NotContain("á", "slug must be ASCII-only");
        result.Should().MatchRegex(@"^[a-z0-9\-]+$");
    }
}
