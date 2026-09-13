namespace CulinaryBlog.Application.Interfaces;

public interface IAuthenticationService
{
    Task<string> GenerateAccessTokenAsync(Guid userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);
    Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}
