using Hangfire;
using CulinaryBlog.Infrastructure.BackgroundJobs;
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
    private readonly string? _publicBaseUrl;
    private readonly GoogleAuthSettings _googleAuthSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IBackgroundJobClient? _backgroundJobs;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IHostEnvironment environment,
        IBackgroundJobClient? backgroundJobs = null)
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

        _publicBaseUrl = configuration["Site:PublicBaseUrl"];
        _logger = logger;
        _environment = environment;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null)
    {
        ValidatePassword(request.Password);
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email)
        {
            throw new CulinaryBlog.Application.Exceptions.ValidationException(
                new Dictionary<string, string[]> { ["email"] = ["Email không hợp lệ."] });
        }

        var userName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim();
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? userName : request.DisplayName.Trim();
        if (userName.Length > 100 || displayName.Length > 100)
        {
            throw new CulinaryBlog.Application.Exceptions.ValidationException(
                new Dictionary<string, string[]> { ["displayName"] = ["Tên đăng nhập và tên hiển thị tối đa 100 ký tự."] });
        }

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
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = ["Author"],
            EmailConfirmed = false
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {UserId}", user.Id);

        var confirmationToken = _jwtTokenGenerator.GenerateEmailConfirmationToken(user, TimeSpan.FromHours(24));
        await SendConfirmationEmailAsync(user, confirmationToken);
        QueueWelcomeEmail(user.Id);

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress = null)
    {
        var login = (string.IsNullOrWhiteSpace(request.Email) ? request.UserNameOrEmail : request.Email)?.Trim() ?? string.Empty;

        _logger.LogInformation("Login attempt for: {Login}", login);

        var user = await _userRepository.GetByEmailOrUserNameAsync(login);

        if (user is null)
        {
            _logger.LogWarning("Login failed: User not found for {Login}", login);
            throw new UnauthorizedAuthException("Invalid username/email or password.");
        }

        _logger.LogDebug("User found: {UserId}, IsActive: {IsActive}, EmailConfirmed: {EmailConfirmed}", 
            user.Id, user.IsActive, user.EmailConfirmed);

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            _logger.LogWarning("Login failed: User {UserId} has no password set (possibly Google-only account)", user.Id);
            throw new UnauthorizedAuthException("This account uses social login. Please use Google to sign in.");
        }

        var now = DateTime.UtcNow;
        if (user.IsActive && user.LockoutEnd > now)
        {
            throw new AccountLockedException(user.LockoutEnd.Value, now);
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            var lockout = await _userRepository.UpdateLoginFailuresAsync(user.Id, failed: true, now);
            if (lockout > now)
            {
                throw new AccountLockedException(lockout.Value, now);
            }

            _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            throw new UnauthorizedAuthException("Invalid username/email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: Account inactive for user {UserId}", user.Id);
            throw new AccountDisabledException();
        }

        // Skip email confirmation check in Development environment for testing
        if (!string.Equals(_environment.EnvironmentName, Environments.Development, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(_environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase)
            && !user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed: Email not confirmed for user {UserId}", user.Id);
            throw new UnauthorizedAuthException("Email not confirmed. Please check your email to confirm your account.");
        }

        var concurrentLockout = await _userRepository.UpdateLoginFailuresAsync(user.Id, failed: false, now);
        if (concurrentLockout > now)
        {
            throw new AccountLockedException(concurrentLockout.Value, now);
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
        if (string.IsNullOrWhiteSpace(request?.IdToken))
        {
            throw new UnauthorizedAuthException("Google ID token is required.", "AUTH_GOOGLE_TOKEN_INVALID");
        }

        if (string.IsNullOrWhiteSpace(_googleAuthSettings.ClientId))
        {
            throw new UnauthorizedAuthException("Google login is not configured.", "AUTH_GOOGLE_TOKEN_INVALID");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleAuthSettings.ClientId]
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (Exception ex) when (ex is not UnauthorizedAuthException)
        {
            _logger.LogWarning("Google token validation failed ({ExceptionType}).", ex.GetType().Name);
            throw new UnauthorizedAuthException("Invalid Google token.", "AUTH_GOOGLE_TOKEN_INVALID");
        }

        if (payload is null || !payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new UnauthorizedAuthException("Invalid Google token.", "AUTH_GOOGLE_TOKEN_INVALID");
        }

        if (!string.IsNullOrEmpty(request.Nonce) && !string.Equals(payload.Nonce, request.Nonce, StringComparison.Ordinal))
        {
            _logger.LogWarning("Google token nonce mismatch.");
            throw new UnauthorizedAuthException("Invalid Google token.", "AUTH_GOOGLE_TOKEN_INVALID");
        }

        var email = payload.Email.ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null)
        {
            // Create new user from Google account
            var generatedUserName = email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N")[..8];
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = generatedUserName,
                Email = email,
                DisplayName = payload.Name ?? email.Split('@')[0],
                AvatarUrl = payload.Picture,
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
                QueueWelcomeEmail(user.Id);
            }
        }
        else
        {
            if (!user.IsActive)
            {
                throw new AccountDisabledException();
            }

            var modified = false;
            if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(payload.Picture))
            {
                user.AvatarUrl = payload.Picture;
                modified = true;
            }

            if (!user.EmailConfirmed && payload.EmailVerified)
            {
                user.EmailConfirmed = true;
                modified = true;
            }

            if (modified)
            {
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        string? ipAddress = null)
    {
        var tokenHash = _jwtTokenGenerator.HashToken(request.RefreshToken);

        var response = await _refreshTokenRepository.WithSessionLockAsync<AuthResponse?>(tokenHash, async storedToken =>
        {
            if (storedToken?.User is not { } user)
            {
                throw new UnauthorizedAuthException("Invalid refresh token.");
            }

            if (!user.IsActive)
            {
                throw new AccountDisabledException();
            }

            if (storedToken.SessionVersion != user.SessionVersion)
            {
                throw new UnauthorizedAuthException("Refresh token belongs to a revoked session.");
            }

            if (storedToken.RevokedAt.HasValue)
            {
                await _refreshTokenRepository.RevokeFamilyAsync(user.Id, storedToken.FamilyId);
                _logger.LogWarning("Refresh token reuse detected for user {UserId}, family {FamilyId}.",
                    user.Id, storedToken.FamilyId);
                return null;
            }

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAuthException("Refresh token has expired.");
            }

            var replacement = await CreateAuthResponseAsync(user, ipAddress, storedToken.FamilyId);
            await _refreshTokenRepository.RevokeAsync(tokenHash, DateTimeOffset.UtcNow,
                _jwtTokenGenerator.HashToken(replacement.RefreshToken));
            return replacement;
        });

        return response ?? throw new UnauthorizedAuthException("Refresh token has been revoked due to reuse detection.");
    }

    public Task LogoutAsync(string refreshToken)
    {
        var tokenHash = _jwtTokenGenerator.HashToken(refreshToken);
        return _refreshTokenRepository.WithSessionLockAsync(tokenHash, async token =>
        {
            if (token is not null)
            {
                await _refreshTokenRepository.RevokeFamilyAsync(token.UserId, token.FamilyId);
            }

            return true;
        });
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
            Bio = profile.Bio,
            EmailConfirmed = profile.EmailConfirmed,
            Roles = profile.Roles,
            IsActive = profile.IsActive,
            CreatedAt = profile.CreatedAt
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

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        ValidatePassword(request.NewPassword);
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword)
                == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAuthException("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _userRepository.UpdateAsync(user);
        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !user.IsActive)
        {
            return;
        }

        var token = _jwtTokenGenerator.GeneratePasswordResetToken(user, TimeSpan.FromHours(1));
        var resetUrl = CreateAccountLink("/reset-password", token);
        try
        {
            await _emailService.SendAsync(new EmailMessage(
                user.Email,
                "Äáº·t láº¡i máº­t kháº©u CulinaryBlog",
                $"<p>ChĂ o {user.DisplayName},</p><p><a href='{resetUrl}'>Äáº·t láº¡i máº­t kháº©u</a></p><p>LiĂªn káº¿t háº¿t háº¡n sau 1 giá».</p>"));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not send password reset email for user {UserId}", user.Id);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        ValidatePassword(request.NewPassword);
        if (!_jwtTokenGenerator.ValidatePasswordResetToken(request.Token, out var userId))
        {
            throw new BadRequestException("Invalid or expired password reset token.");
        }

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _userRepository.UpdateAsync(user);
        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
        await _unitOfWork.SaveChangesAsync();
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

        await _userRepository.UpdateAsync(targetUser);
        targetUser.IsActive = isActive;
        targetUser.SessionVersion = Guid.NewGuid();

        // If deactivating, revoke all refresh tokens
        if (!isActive)
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(targetUserId);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateAuthorRoleAsync(Guid adminUserId, Guid targetUserId, bool enabled)
    {
        var adminUser = await _userRepository.GetByIdAsync(adminUserId);
        if (adminUser is null || !adminUser.Roles.Contains("Admin"))
        {
            throw new ForbiddenException("Only administrators can change author roles.");
        }

        var targetUser = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new NotFoundException("User not found.");
        var roles = (targetUser.Roles ?? []).ToList();
        var hasAuthorRole = roles.Contains("Author", StringComparer.OrdinalIgnoreCase);

        if (enabled && !hasAuthorRole)
        {
            roles.Add("Author");
        }
        else if (!enabled && hasAuthorRole)
        {
            roles.RemoveAll(role => role.Equals("Author", StringComparison.OrdinalIgnoreCase));
        }

        if (!roles.Any(role => !role.Equals("Author", StringComparison.OrdinalIgnoreCase)))
        {
            roles.Add("User");
        }

        if (hasAuthorRole == enabled)
        {
            return false;
        }

        targetUser.Roles = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        await _userRepository.UpdateAsync(targetUser);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }


    private void QueueWelcomeEmail(Guid userId)
    {
        try
        {
            _backgroundJobs?.Enqueue<WelcomeEmailJob>(job => job.RunAsync(userId, CancellationToken.None));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not queue welcome email for user {UserId}.", userId);
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit)
            || !password.Any(character => char.IsPunctuation(character) || char.IsSymbol(character)))
        {
            throw new CulinaryBlog.Application.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    ["password"] = ["Password must be at least 8 characters and contain uppercase, lowercase, digit, and special character."]
                });
        }
    }

    private string CreateAccountLink(string path, string token)
    {
        if (!Uri.TryCreate(_publicBaseUrl, UriKind.Absolute, out var origin) ||
            (origin.Scheme != Uri.UriSchemeHttps &&
                (origin.Scheme != Uri.UriSchemeHttp || _environment.IsProduction())) ||
            !string.IsNullOrEmpty(origin.UserInfo) ||
            !string.IsNullOrEmpty(origin.Query) ||
            !string.IsNullOrEmpty(origin.Fragment))
        {
            throw new InvalidOperationException("Configure Site:PublicBaseUrl with the public frontend URL.");
        }

        return $"{origin.AbsoluteUri.TrimEnd('/')}{path}?token={Uri.EscapeDataString(token)}";
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user, string token)
    {
        try
        {
            var confirmUrl = CreateAccountLink("/confirm-email", token);
            
            var subject = "XĂ¡c thá»±c email tĂ i khoáº£n CulinaryBlog";
            var htmlBody = $@"
                <h1>XĂ¡c thá»±c email</h1>
                <p>ChĂ o {user.DisplayName},</p>
                <p>Vui lĂ²ng nháº¥p vĂ o liĂªn káº¿t bĂªn dÆ°á»›i Ä‘á»ƒ xĂ¡c thá»±c email cá»§a báº¡n:</p>
                <p><a href='{confirmUrl}'>XĂ¡c thá»±c email</a></p>
                <p>LiĂªn káº¿t nĂ y sáº½ háº¿t háº¡n sau 24 giá».</p>
                <hr>
                <p><small>ÄĂ¢y lĂ  email tá»± Ä‘á»™ng, vui lĂ²ng khĂ´ng tráº£ lá»i.</small></p>";

            await _emailService.SendAsync(new EmailMessage(user.Email, subject, htmlBody));
        }
        catch
        {
            // Log error but don't fail
        }
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user, string? ipAddress, Guid? familyId = null)
    {
        var accessTokenExpiry = TimeSpan.FromMinutes(_jwtSettings.AccessTokenMinutes);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, user.Roles ?? [], accessTokenExpiry);
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _jwtTokenGenerator.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId ?? Guid.NewGuid(),
            SessionVersion = user.SessionVersion,
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
            Bio = user.Bio,
            EmailConfirmed = user.EmailConfirmed,
            Roles = user.Roles ?? [],
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}

public sealed class GoogleAuthSettings
{
    public const string Section = "GoogleAuth";
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
