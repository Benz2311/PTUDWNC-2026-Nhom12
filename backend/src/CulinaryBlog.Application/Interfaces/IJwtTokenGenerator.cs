using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles, TimeSpan expiry);
    string GenerateRefreshToken();
    string HashToken(string token);
    string GenerateEmailConfirmationToken(ApplicationUser user, TimeSpan expiry);
    bool ValidateEmailConfirmationToken(string token, out Guid userId);
}