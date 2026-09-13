using CulinaryBlog.Application.Interfaces;

namespace CulinaryBlog.Infrastructure.Authentication;

public class JwtTokenService : IAuthenticationService
{
    public Task<string> GenerateAccessTokenAsync(Guid userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var roleList = roles.ToList();
        var token = $"access-token-for-{userId}-{string.Join('-', roleList)}";
        return Task.FromResult(token);
    }

    public Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var token = $"refresh-token-for-{userId}-{Guid.NewGuid():N}";
        return Task.FromResult(token);
    }
}
