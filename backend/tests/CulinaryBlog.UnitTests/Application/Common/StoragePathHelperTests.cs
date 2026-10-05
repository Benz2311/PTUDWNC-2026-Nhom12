using CulinaryBlog.Application.Common.Utilities;
using FluentAssertions;

namespace CulinaryBlog.UnitTests.Application.Common;

public class StoragePathHelperTests
{
    [Theory]
    [InlineData("recipes/123/images/sample.jpg", "recipes/123/images/sample.jpg")]
    [InlineData("recipes\\123\\images\\sample.jpg", "recipes/123/images/sample.jpg")]
    [InlineData("avatars/user-99.png", "avatars/user-99.png")]
    public void SanitizeKey_WithValidKey_ReturnsNormalizedPath(string inputKey, string expected)
    {
        var result = StoragePathHelper.SanitizeKey(inputKey);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeKey_WithNullOrWhitespace_ThrowsArgumentException(string? inputKey)
    {
        var act = () => StoragePathHelper.SanitizeKey(inputKey!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*không được để trống*");
    }

    [Theory]
    [InlineData("../../secret.txt")]
    [InlineData("../passwords.env")]
    [InlineData("recipes/../../appsettings.json")]
    [InlineData("recipes/./evil.jpg")]
    [InlineData("/etc/passwd")]
    [InlineData("\\windows\\system32")]
    public void SanitizeKey_WithPathTraversal_ThrowsArgumentException(string maliciousKey)
    {
        var act = () => StoragePathHelper.SanitizeKey(maliciousKey);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Path Traversal*");
    }

    [Fact]
    public void SanitizeKey_WithDriveLetter_ThrowsArgumentException()
    {
        var act = () => StoragePathHelper.SanitizeKey("C:/inetpub/wwwroot/shell.aspx");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*drive letter*");
    }

    [Fact]
    public void SanitizeKey_WithControlCharacters_ThrowsArgumentException()
    {
        var act = () => StoragePathHelper.SanitizeKey("recipes/images\0hidden.jpg");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ký tự điều khiển*");
    }

    [Theory]
    [InlineData("pho-bo.jpg", ".jpg")]
    [InlineData("ANH_DEP.JPEG", ".jpeg")]
    [InlineData("dish.PNG", ".png")]
    [InlineData("food.webp", ".webp")]
    public void SanitizeExtension_WithValidFileName_ReturnsSafeLowerExtension(string fileName, string expectedExt)
    {
        var result = StoragePathHelper.SanitizeExtension(fileName);
        result.Should().Be(expectedExt);
    }

    [Theory]
    [InlineData("trojan.exe", ".jpg")]
    [InlineData("malware.sh", ".jpg")]
    [InlineData("../../etc/passwd", ".jpg")]
    [InlineData("noextension", ".jpg")]
    [InlineData("", ".jpg")]
    [InlineData(null, ".jpg")]
    public void SanitizeExtension_WithDangerousOrInvalidFileName_DefaultsToJpg(string? dangerousFileName, string expectedExt)
    {
        var result = StoragePathHelper.SanitizeExtension(dangerousFileName);
        result.Should().Be(expectedExt);
    }

    [Fact]
    public void GenerateRecipeImageKey_GeneratesUniqueServerControlledKey()
    {
        var recipeId = Guid.NewGuid();

        var key1 = StoragePathHelper.GenerateRecipeImageKey(recipeId, ".jpg");
        var key2 = StoragePathHelper.GenerateRecipeImageKey(recipeId, ".jpg");

        key1.Should().NotBe(key2);
        key1.Should().StartWith($"recipes/{recipeId}/images/");
        key2.Should().StartWith($"recipes/{recipeId}/images/");
        key1.Should().EndWith(".jpg");
        key2.Should().EndWith(".jpg");
    }

    [Fact]
    public void GenerateRecipeImageKey_DoesNotUseClientRawFileName()
    {
        var recipeId = Guid.NewGuid();
        var clientFileName = "../../malicious-pho-bo-hack.php";
        var safeExtension = StoragePathHelper.SanitizeExtension(clientFileName);

        var key = StoragePathHelper.GenerateRecipeImageKey(recipeId, safeExtension);

        key.Should().NotContain("malicious");
        key.Should().NotContain("hack");
        key.Should().NotContain("..");
        key.Should().EndWith(".jpg");
    }
}
