using CulinaryBlog.Application.Repositories;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _db;

    public RefreshTokenRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashWithUserAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _db.RefreshTokens
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _db.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task RevokeAsync(string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken cancellationToken = default)
    {
        await _db.RefreshTokens
            .Where(x => x.TokenHash == tokenHash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAt, revokedAt)
                .SetProperty(x => x.ReplacedByTokenHash, replacedByTokenHash), cancellationToken);
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await _db.RefreshTokens
            .Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
    }
}