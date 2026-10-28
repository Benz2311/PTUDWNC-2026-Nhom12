using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.UnitTests;

public class RefreshTokenCompatibilityTests
{
    [Fact]
    public void TokenHashAndToken_AreSynced()
    {
        var token = new RefreshToken
        {
            TokenHash = "hash-123",
            ReplacedByTokenHash = "replacement-456"
        };

        token.Token.Should().Be("hash-123");
        token.ReplacedByToken.Should().Be("replacement-456");

        token.Token = "updated-hash";
        token.ReplacedByToken = "updated-replacement";

        token.TokenHash.Should().Be("updated-hash");
        token.ReplacedByTokenHash.Should().Be("updated-replacement");
    }
}
