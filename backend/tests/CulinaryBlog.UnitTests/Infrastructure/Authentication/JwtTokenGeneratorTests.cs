using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CulinaryBlog.UnitTests.Infrastructure.Authentication;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void ValidateEmailConfirmationToken_ReturnsUserIdForValidToken()
    {
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-at-least-32-characters-long",
            Issuer = "test-issuer",
            Audience = "test-audience"
        });
        var generator = new JwtTokenGenerator(settings);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "student@dlu.edu.vn",
            UserName = "student"
        };
        var token = generator.GenerateEmailConfirmationToken(user, TimeSpan.FromMinutes(5));

        var isValid = generator.ValidateEmailConfirmationToken(token, out var userId);

        isValid.Should().BeTrue();
        userId.Should().Be(user.Id);
    }

    [Fact]
    public void ValidateEmailConfirmationToken_RejectsAccessToken()
    {
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-at-least-32-characters-long",
            Issuer = "test-issuer",
            Audience = "test-audience"
        });
        var generator = new JwtTokenGenerator(settings);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "student@dlu.edu.vn",
            UserName = "student"
        };
        var token = generator.GenerateAccessToken(user, ["Author"], TimeSpan.FromMinutes(5));

        var isValid = generator.ValidateEmailConfirmationToken(token, out var userId);

        isValid.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ValidatePasswordResetToken_ReturnsUserIdForValidToken()
    {
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-at-least-32-characters-long",
            Issuer = "test-issuer",
            Audience = "test-audience"
        });
        var generator = new JwtTokenGenerator(settings);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "student@dlu.edu.vn",
            UserName = "student"
        };
        var token = generator.GeneratePasswordResetToken(user, TimeSpan.FromMinutes(5));

        var isValid = generator.ValidatePasswordResetToken(token, out var userId);

        isValid.Should().BeTrue();
        userId.Should().Be(user.Id);
    }

    [Fact]
    public void ValidatePasswordResetToken_RejectsEmailConfirmationToken()
    {
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-at-least-32-characters-long",
            Issuer = "test-issuer",
            Audience = "test-audience"
        });
        var generator = new JwtTokenGenerator(settings);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "student@dlu.edu.vn",
            UserName = "student"
        };
        var token = generator.GenerateEmailConfirmationToken(user, TimeSpan.FromMinutes(5));

        var isValid = generator.ValidatePasswordResetToken(token, out var userId);

        isValid.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }
}
