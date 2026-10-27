using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.Repositories;
using CulinaryBlog.Application.Services;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Exceptions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly GoogleAuthSettings _googleAuthSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IHostEnvironment _environment;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IHostEnvironment environment)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _jwtSettings = configuration
            .GetSection(JwtSettings.Section)
            .Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt settings not configured.");
        
        _googleAuthSettings = configuration
            .GetSection(GoogleAuthSettings.Section)
            .Get<GoogleAuthSettings>() ?? new GoogleAuthSettings();

        _logger = logger;
        _environment = environment;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        _logger.LogInformation("Registering new user: {UserName} ({Email})", userName, email);

        if (await _userRepository.ExistsByUserNameAsync(userName))
        {
            _logger.LogWarning("Registration failed: Username '{UserName}' already exists", userName);
            throw new ConflictException($"Username '{userName}' already exists.");
        }

        if (await _userRepository.ExistsByEmailAsync(email))
        {
            _logger.LogWarning("Registration failed: Email '{Email}' already exists", email);
            throw new ConflictException($"Email '{email}' already exists.");
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
            Roles = ["Author"],
            EmailConfirmed = false
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {UserId}", user.Id);

        // Send welcome email with confirmation link
        await SendWelcomeEmailAsync(user);

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress = null)
    {
        var login = request.UserNameOrEmail.Trim();

        _logger.LogInformation("Login attempt for: {Login}", login);

        var user = await _userRepository.GetByEmailOrUserNameAsync(login);

        if (user is null)
        {
            _logger.LogWarning("Login failed: User not found for {Login}", login);
            throw new UnauthorizedAuthException("Invalid username/email or password.");
        }

        _logger.LogDebug("User found: {UserId}, IsActive: {IsActive}, EmailConfirmed: {EmailConfirmed}", 
            user.Id, user.IsActive, user.EmailConfirmed);

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: Account inactive for user {UserId}", user.Id);
            throw new UnauthorizedAuthException("Account is inactive.");
        }

        // Skip email confirmation check in Development environment for testing
        if (!string.Equals(_environment.EnvironmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) && !user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed: Email not confirmed for user {UserId}", user.Id);
            throw new UnauthorizedAuthException("Email not confirmed. Please check your email to confirm your account.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            _logger.LogWarning("Login failed: User {UserId} has no password set (possibly Google-only account)", user.Id);
            throw new UnauthorizedAuthException("This account uses social login. Please use Google to sign in.");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            throw new UnauthorizedAuthException("Invalid username/email or password.");
        }

        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            _logger.LogInformation("Rehashing password for user {UserId}", user.Id);
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("Login successful for user {UserId}", user.Id);

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> GoogleLoginAsync(
        GoogleLoginRequest request,
        string? ipAddress = null)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _googleAuthSettings.ClientId }
        });

        if (payload == null)
        {
            throw new UnauthorizedAuthException("Invalid Google token.");
        }

        var email = payload.Email.ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null)
        {
            // Create new user from Google account
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N")[..8],
                Email = email,
                DisplayName = payload.Name ?? email.Split('@')[0],
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Roles = ["Author"],
                EmailConfirmed = payload.EmailVerified
            };

            // Generate a random password since Google users don't use password login
            var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            user.PasswordHash = _passwordHasher.HashPassword(user, randomPassword);

            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            if (user.EmailConfirmed)
            {
                await SendWelcomeEmailAsync(user);
            }
        }
        else if (!user.IsActive)
        {
            throw new UnauthorizedAuthException("Account is inactive.");
        }

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        string? ipAddress = null)
    {
        var tokenHash = _jwtTokenGenerator.HashToken(request.RefreshToken);

        var storedToken = await _refreshTokenRepository.GetByTokenHashWithUserAsync(tokenHash);

        if (storedToken is null)
        {
            throw new UnauthorizedAuthException("Invalid refresh token.");
        }

        if (storedToken.RevokedAt.HasValue)
        {
            // Token reuse detection - revoke all tokens in the family
            await _refreshTokenRepository.RevokeAllForUserAsync(storedToken.UserId);
            await _unitOfWork.SaveChangesAsync();
            throw new UnauthorizedAuthException("Refresh token has been revoked due to reuse detection.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAuthException("Refresh token has expired.");
        }

        var response = await CreateAuthResponseAsync(storedToken.User, ipAddress);

        await _refreshTokenRepository.RevokeAsync(tokenHash, DateTimeOffset.UtcNow, _jwtTokenGenerator.HashToken(response.RefreshToken));
        await _unitOfWork.SaveChangesAsync();

        return response;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var tokenHash = _jwtTokenGenerator.HashToken(refreshToken);
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

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        if (!_jwtTokenGenerator.ValidateEmailConfirmationToken(request.Token, out var userId))
        {
            throw new BadRequestException("Invalid or expired confirmation token.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return; // Already confirmed, idempotent
        }

        user.EmailConfirmed = true;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Send welcome email if not sent before
        await SendWelcomeEmailAsync(user);
    }

    public async Task ResendConfirmationEmailAsync(ResendConfirmationEmailRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email);

        // Always return success to prevent user enumeration
        if (user is null || user.EmailConfirmed)
        {
            return;
        }

        if (!user.IsActive)
        {
            return; // Don't send to inactive accounts
        }

        var token = _jwtTokenGenerator.GenerateEmailConfirmationToken(user, TimeSpan.FromHours(24));
        await SendConfirmationEmailAsync(user, token);
    }

    public async Task<UserResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.AvatarUrl != null)
        {
            user.AvatarUrl = request.AvatarUrl.Trim();
        }

        if (request.Bio != null)
        {
            user.Bio = request.Bio.Trim();
        }

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return ToUserResponse(user);
    }

    public async Task<bool> UpdateUserStatusAsync(Guid adminUserId, Guid targetUserId, bool isActive)
    {
        // Prevent admin from deactivating themselves
        if (adminUserId == targetUserId)
        {
            throw new BadRequestException("Cannot change your own account status.");
        }

        var targetUser = await _userRepository.GetByIdAsync(targetUserId);
        if (targetUser is null)
        {
            throw new NotFoundException("User not found.");
        }

        var adminUser = await _userRepository.GetByIdAsync(adminUserId);
        if (adminUser is null || !adminUser.Roles.Contains("Admin"))
        {
            throw new ForbiddenException("Only administrators can change user status.");
        }

        if (targetUser.IsActive == isActive)
        {
            return false; // No change
        }

        targetUser.IsActive = isActive;
        await _userRepository.UpdateAsync(targetUser);

        // If deactivating, revoke all refresh tokens
        if (!isActive)
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(targetUserId);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task SendWelcomeEmailAsync(ApplicationUser user)
    {
        try
        {
            var subject = "Chào mừng bạn đến với CulinaryBlog!";
            var htmlBody = $@"
                <h1>Chào {user.DisplayName},</h1>
                <p>Cảm ơn bạn đã đăng ký tài khoản tại CulinaryBlog.</p>
                <p>Chúc bạn có những trải nghiệm nấu ăn tuyệt vời!</p>
                <hr>
                <p><small>Đây là email tự động, vui lòng không trả lời.</small></p>";

            await _emailService.SendAsync(new EmailMessage(user.Email, subject, htmlBody));
        }
        catch
        {
            // Log error but don't fail registration
        }
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user, string token)
    {
        try
        {
            var confirmUrl = $"{_jwtSettings.Issuer.Replace("api", "").TrimEnd('/')}/auth/confirm-email?token={Uri.EscapeDataString(token)}";
            
            var subject = "Xác thực email tài khoản CulinaryBlog";
            var htmlBody = $@"
                <h1>Xác thực email</h1>
                <p>Chào {user.DisplayName},</p>
                <p>Vui lòng nhấp vào liên kết bên dưới để xác thực email của bạn:</p>
                <p><a href='{confirmUrl}'>Xác thực email</a></p>
                <p>Liên kết này sẽ hết hạn sau 24 giờ.</p>
                <hr>
                <p><small>Đây là email tự động, vui lòng không trả lời.</small></p>";

            await _emailService.SendAsync(new EmailMessage(user.Email, subject, htmlBody));
        }
        catch
        {
            // Log error but don't fail
        }
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user, string? ipAddress)
    {
        var accessTokenExpiry = TimeSpan.FromMinutes(_jwtSettings.AccessTokenMinutes);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, user.Roles ?? [], accessTokenExpiry);
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _jwtTokenGenerator.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        var expiresAt = DateTimeOffset.UtcNow.Add(accessTokenExpiry);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt,
            User = ToUserResponse(user)
        };
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

public sealed class GoogleAuthSettings
{
    public const string Section = "GoogleAuth";
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}