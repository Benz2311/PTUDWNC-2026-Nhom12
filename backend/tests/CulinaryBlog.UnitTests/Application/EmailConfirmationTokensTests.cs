using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Api.Endpoints.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.UnitTests.Application;

public class EmailConfirmationTokensTests
{
    private const string SigningKey = "test-email-confirmation-signing-key-with-at-least-32-bytes";

    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = SigningKey,
            ["Jwt:Issuer"] = "CulinaryBlogTest",
            ["Jwt:Audience"] = "CulinaryBlogClientTest"
        })
        .Build();

    [Fact]
    public void CreateAndValidate_ReturnsBoundUserAndEmail()
    {
        var userId = Guid.NewGuid();
        const string email = "chef@example.com";

        var token = EmailConfirmationTokens.Create(userId, email, _configuration);
        var valid = EmailConfirmationTokens.TryValidate(token, _configuration, out var actualUserId, out var actualEmail);

        valid.Should().BeTrue();
        actualUserId.Should().Be(userId);
        actualEmail.Should().Be(email);
    }

    [Fact]
    public void TryValidate_RejectsTokenSignedByAnotherKey()
    {
        var token = EmailConfirmationTokens.Create(Guid.NewGuid(), "chef@example.com", _configuration);
        var otherConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "another-test-signing-key-with-at-least-32-bytes",
                ["Jwt:Issuer"] = "CulinaryBlogTest",
                ["Jwt:Audience"] = "CulinaryBlogClientTest"
            })
            .Build();

        EmailConfirmationTokens.TryValidate(token, otherConfiguration, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryValidate_RejectsExpiredToken()
    {
        var expiredToken = new JwtSecurityToken(
            issuer: "CulinaryBlogTest",
            audience: "CulinaryBlogClientTest",
            claims:
            [
                new Claim("email_confirmation_user_id", Guid.NewGuid().ToString()),
                new Claim("email_confirmation_email", "chef@example.com"),
                new Claim("purpose", "email_confirmation")
            ],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256));
        var token = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        EmailConfirmationTokens.TryValidate(token, _configuration, out _, out _).Should().BeFalse();
    }
}