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

    Task<AuthResponse> GoogleLoginAsync(
        GoogleLoginRequest request,
        string? ipAddress = null);

    Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        string? ipAddress = null);

    Task LogoutAsync(string refreshToken);

    Task<UserResponse?> GetMeAsync(string userId);

    Task ConfirmEmailAsync(ConfirmEmailRequest request);

    Task ResendConfirmationEmailAsync(ResendConfirmationEmailRequest request);

    Task<UserResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request);

    Task<bool> UpdateUserStatusAsync(Guid adminUserId, Guid targetUserId, bool isActive);
}
