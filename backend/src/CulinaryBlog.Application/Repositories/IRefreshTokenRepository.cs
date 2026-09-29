using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenHashWithUserAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
}