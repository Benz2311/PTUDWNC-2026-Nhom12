using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.Repositories;
using CulinaryBlog.Application.Services;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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
        {
            throw new InvalidOperationException("Username already exists.");
        }

        if (await _userRepository.ExistsByEmailAsync(email))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? userName
                : request.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = ["Author"]
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
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive.");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        string? ipAddress = null)
    {
        var tokenHash = HashToken(request.RefreshToken);

        var storedToken = await _refreshTokenRepository.GetByTokenHashWithUserAsync(tokenHash);

        if (storedToken is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (storedToken.RevokedAt.HasValue)
        {
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

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
        {
            return null;
        }

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
        var rawRefreshToken = GenerateRefreshToken();
        var hashedRefreshToken = HashToken(rawRefreshToken);

        var refreshTokenDays = _configuration.GetValue<int>("Jwt:RefreshTokenDays", 7);
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        var accessTokenMinutes = _configuration.GetValue<int>(
            "Jwt:AccessTokenMinutes",
            _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60));

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(accessTokenMinutes);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt,
            User = ToUserResponse(user)
        };
    }

    private string GenerateAccessToken(ApplicationUser user)
    {
        var key = _configuration["Jwt:Key"]
            ?? _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT key is missing.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("displayName", user.DisplayName)
        };

        if (user.Roles != null && user.Roles.Length > 0)
        {
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var accessTokenMinutes = _configuration.GetValue<int>(
            "Jwt:AccessTokenMinutes",
            _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60));

        var expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes);

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
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio
        };
    }
}
