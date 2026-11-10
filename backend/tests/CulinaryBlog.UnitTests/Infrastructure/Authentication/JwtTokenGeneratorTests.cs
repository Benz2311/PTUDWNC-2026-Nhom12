using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CulinaryBlog.UnitTests.Infrastructure.Authentication;

public class JwtTokenGeneratorTests
{
    private readonly JwtSettings _settings;
    private readonly JwtTokenGenerator _sut;

    public JwtTokenGeneratorTests()
    {
        _settings = new JwtSettings
        {
            SecretKey = "a-very-long-secret-key-that-is-at-least-32-bytes-long!",
            Issuer = "CulinaryBlogTestIssuer",
            Audience = "CulinaryBlogTestAudience",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };

        var options = Options.Create(_settings);
        _sut = new JwtTokenGenerator(options);
    }

    [Fact]
    public void ValidateEmailConfirmationToken_ValidToken_ReturnsTrueAndCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        var token = _sut.GenerateEmailConfirmationToken(user, TimeSpan.FromMinutes(15));

        // Act
        var isValid = _sut.ValidateEmailConfirmationToken(token, out var extractedUserId);

        // Assert
        isValid.Should().BeTrue();
        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void ValidateEmailConfirmationToken_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        // Create an already-expired token
        var token = _sut.GenerateEmailConfirmationToken(user, TimeSpan.FromSeconds(-5));

        // Act
        var isValid = _sut.ValidateEmailConfirmationToken(token, out var extractedUserId);

        // Assert
        isValid.Should().BeFalse();
        extractedUserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ValidateEmailConfirmationToken_TamperedSignature_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        var token = _sut.GenerateEmailConfirmationToken(user, TimeSpan.FromMinutes(15));
        var tamperedToken = token[..^4] + "ABCD";

        // Act
        var isValid = _sut.ValidateEmailConfirmationToken(tamperedToken, out var extractedUserId);

        // Assert
        isValid.Should().BeFalse();
        extractedUserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ValidatePasswordResetToken_ValidToken_ReturnsTrueAndCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        var token = _sut.GeneratePasswordResetToken(user, TimeSpan.FromMinutes(30));

        // Act
        var isValid = _sut.ValidatePasswordResetToken(token, out var extractedUserId);

        // Assert
        isValid.Should().BeTrue();
        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void ValidatePasswordResetToken_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        // Create an already-expired token
        var token = _sut.GeneratePasswordResetToken(user, TimeSpan.FromSeconds(-10));

        // Act
        var isValid = _sut.ValidatePasswordResetToken(token, out var extractedUserId);

        // Assert
        isValid.Should().BeFalse();
        extractedUserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ValidatePasswordResetToken_TamperedSignature_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        var token = _sut.GeneratePasswordResetToken(user, TimeSpan.FromMinutes(30));
        var tamperedToken = token[..^4] + "WXYZ";

        // Act
        var isValid = _sut.ValidatePasswordResetToken(tamperedToken, out var extractedUserId);

        // Assert
        isValid.Should().BeFalse();
        extractedUserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void CrossPurpose_EmailConfirmationToken_CannotBeUsedForPasswordReset()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        var emailToken = _sut.GenerateEmailConfirmationToken(user, TimeSpan.FromMinutes(15));

        // Act
        var isValid = _sut.ValidatePasswordResetToken(emailToken, out var extractedUserId);

        // Assert
        isValid.Should().BeFalse();
        extractedUserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void CrossPurpose_PasswordResetToken_CannotBeUsedForEmailConfirmation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        var resetToken = _sut.GeneratePasswordResetToken(user, TimeSpan.FromMinutes(15));

        // Act
        var isValid = _sut.ValidateEmailConfirmationToken(resetToken, out var extractedUserId);

        // Assert
        isValid.Should().BeFalse();
        extractedUserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void AccessToken_CannotBeUsedForPasswordResetOrEmailConfirmation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testauthor",
            Email = "author@test.com",
            DisplayName = "Test Author"
        };

        var accessToken = _sut.GenerateAccessToken(user, ["Author"], TimeSpan.FromMinutes(15));

        // Act & Assert
        _sut.ValidatePasswordResetToken(accessToken, out _).Should().BeFalse();
        _sut.ValidateEmailConfirmationToken(accessToken, out _).Should().BeFalse();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsValid32ByteBase64UrlString()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        var bytes = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(token);
        bytes.Length.Should().Be(32);
    }

    [Fact]
    public void HashToken_ProducesDeterministicSha256HexString()
    {
        // Arrange
        const string rawToken = "my-sample-refresh-token-12345";

        // Act
        var hash1 = _sut.HashToken(rawToken);
        var hash2 = _sut.HashToken(rawToken);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64); // SHA-256 produces 32 bytes -> 64 hex characters
    }
}
