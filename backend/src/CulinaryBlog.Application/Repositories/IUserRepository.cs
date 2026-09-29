using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailOrUserNameAsync(string emailOrUserName, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
}

public record UserProfileDto(
    string Id,
    string UserName,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Bio);