using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Repositories;
using CulinaryBlog.Application.Services;
using CulinaryBlog.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.ExistsByUserNameAsync(userName))
            throw new InvalidOperationException("Username already exists.");

        if (await _userRepository.ExistsByEmailAsync(email))
            throw new InvalidOperationException("Email already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? userName
                : request.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress = null)
    {
        var login = request.UserNameOrEmail.Trim();

        var user = await _userRepository.GetByEmailOrUserNameAsync(login);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid username/email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive.");

        var userForVerification = new ApplicationUser
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PasswordHash = user.PasswordHash
        };

        if (_passwordHasher.VerifyHashedPassword(
                userForVerification,
                user.PasswordHash!,
                request.Password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid username/email or password.");

        var fullUser = new ApplicationUser
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            IsActive = user.IsActive,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio
        };

        return await CreateAuthResponseAsync(fullUser, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        string? ipAddress = null)
    {
        var tokenHash = HashToken(request.RefreshToken);

        var storedToken = await _refreshTokenRepository.GetByTokenHashWithUserAsync(tokenHash);

        if (storedToken is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (storedToken.RevokedAt.HasValue)
            throw new UnauthorizedAccessException("Refresh token has been revoked.");

        if (storedToken.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        var response = await CreateAuthResponseAsync(storedToken.User, ipAddress);

        await _refreshTokenRepository.RevokeAsync(
            tokenHash,
            DateTimeOffset.UtcNow,
            HashToken(response.RefreshToken));

        await _unitOfWork.SaveChangesAsync();

        return response;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        await _refreshTokenRepository.RevokeAsync(tokenHash, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<UserResponse?> GetMeAsync(string userId)
    {
        var profile = await _userRepository.GetProfileAsync(userId);

        if (profile is null)
            return null;

        return new UserResponse
        {
            Id = profile.Id,
            UserName = profile.UserName,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            Bio = profile.Bio
        };
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        ApplicationUser user,
        string? ipAddress)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(
                _configuration.GetValue<int>("Jwt:RefreshTokenDays", 7)),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(
            _configuration.GetValue<int>("Jwt:AccessTokenMinutes", 60));

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = ToUserResponse(user)
        };
    }

    private string GenerateAccessToken(ApplicationUser user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is missing.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("displayName", user.DisplayName)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(
            _configuration.GetValue<int>("Jwt:AccessTokenMinutes", 60));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static UserResponse ToUserResponse(ApplicationUser user)
    {
        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio
        };
    }
}