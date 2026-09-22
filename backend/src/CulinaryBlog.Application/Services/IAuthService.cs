using CulinaryBlog.Application.DTOs.Auth;

namespace CulinaryBlog.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null);

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress = null);

    Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        string? ipAddress = null);

    Task LogoutAsync(string refreshToken);

    Task<UserResponse?> GetMeAsync(Guid userId);
}
