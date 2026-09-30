using CulinaryBlog.Application.Repositories;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (Guid.TryParse(id, out var guid))
        {
            return GetByIdAsync(guid, cancellationToken);
        }

        return Task.FromResult<ApplicationUser?>(null);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var lower = email.Trim().ToLowerInvariant();
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == lower, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var lower = userName.Trim().ToLowerInvariant();
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName.ToLower() == lower, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByEmailOrUserNameAsync(string emailOrUserName, CancellationToken cancellationToken = default)
    {
        var lower = emailOrUserName.Trim().ToLowerInvariant();
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName.ToLower() == lower || x.Email.ToLower() == lower, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var lower = email.Trim().ToLowerInvariant();
        return await _db.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email.ToLower() == lower, cancellationToken);
    }

    public async Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var lower = userName.Trim().ToLowerInvariant();
        return await _db.Users
            .AsNoTracking()
            .AnyAsync(x => x.UserName.ToLower() == lower, cancellationToken);
    }

    public async Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        await _db.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new UserProfileDto(
                x.Id.ToString(),
                x.UserName,
                x.Email,
                x.DisplayName,
                x.AvatarUrl,
                x.Bio))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (Guid.TryParse(userId, out var guid))
        {
            return GetProfileAsync(guid, cancellationToken);
        }

        return Task.FromResult<UserProfileDto?>(null);
    }
}
