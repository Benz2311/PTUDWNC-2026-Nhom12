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
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (token != null)
        {
            token.RevokedAt = revokedAt.UtcDateTime;
            token.ReplacedByTokenHash = replacedByTokenHash;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = nowUtc;
        }

        if (tokens.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (Guid.TryParse(userId, out var guid))
        {
            return RevokeAllForUserAsync(guid, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var expiredTokens = await _db.RefreshTokens
            .Where(x => x.ExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);

        if (expiredTokens.Count > 0)
        {
            _db.RefreshTokens.RemoveRange(expiredTokens);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return expiredTokens.Count;
    }
}
