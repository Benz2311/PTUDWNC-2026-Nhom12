using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.Repositories;
using CulinaryBlog.Application.Services;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Exceptions;
using CulinaryBlog.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenRepository> _tokenRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<IPasswordHasher<ApplicationUser>> _hasherMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly Mock<IHostEnvironment> _environmentMock = new();
    private readonly AuthService _sut;

public AuthServiceTests()
    {
        _environmentMock.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);

        var jwtSettings = new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-very-long-enough-for-testing",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };

        var googleAuthSettings = new GoogleAuthSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{JwtSettings.Section}:SecretKey"] = jwtSettings.SecretKey,
            [$"{JwtSettings.Section}:Issuer"] = jwtSettings.Issuer,
            [$"{JwtSettings.Section}:Audience"] = jwtSettings.Audience,
            [$"{JwtSettings.Section}:AccessTokenMinutes"] = jwtSettings.AccessTokenMinutes.ToString(),
            [$"{JwtSettings.Section}:RefreshTokenDays"] = jwtSettings.RefreshTokenDays.ToString(),
            ["Frontend:BaseUrl"] = "https://culinaryblog.example",
            [$"{GoogleAuthSettings.Section}:ClientId"] = googleAuthSettings.ClientId,
            [$"{GoogleAuthSettings.Section}:ClientSecret"] = googleAuthSettings.ClientSecret,
        });

        _configuration = configBuilder.Build();

        _jwtTokenGeneratorMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>(), It.IsAny<TimeSpan>()))
            .Returns("test_access_token");
        _jwtTokenGeneratorMock.Setup(x => x.GenerateRefreshToken())
            .Returns("test_refresh_token");
        _jwtTokenGeneratorMock.Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns("hashed_token");

        _sut = new AuthService(
            _userRepoMock.Object,
            _tokenRepoMock.Object,
            _uowMock.Object,
            _jwtTokenGeneratorMock.Object,
            _hasherMock.Object,
            _emailServiceMock.Object,
            _configuration,
            _loggerMock.Object,
            _environmentMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            UserName = "chef_john",
            Email = "john@example.com",
            Password = "Password123!",
            DisplayName = "Chef John"
        };

        _userRepoMock.Setup(x => x.ExistsByUserNameAsync("chef_john", default)).ReturnsAsync(false);
        _userRepoMock.Setup(x => x.ExistsByEmailAsync("john@example.com", default)).ReturnsAsync(false);
        _hasherMock.Setup(x => x.HashPassword(It.IsAny<ApplicationUser>(), "Password123!")).Returns("hashed_pwd");
        _jwtTokenGeneratorMock.Setup(x => x.GenerateEmailConfirmationToken(It.IsAny<ApplicationUser>(), It.IsAny<TimeSpan>()))
            .Returns("email-confirmation-token");

        // Act
        var result = await _sut.RegisterAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.UserName.Should().Be("chef_john");
        result.User.Email.Should().Be("john@example.com");

        _userRepoMock.Verify(x => x.AddAsync(It.Is<ApplicationUser>(u =>
            u.UserName == "chef_john" &&
            u.PasswordHash == "hashed_pwd" &&
            !u.EmailConfirmed), default), Times.Once);
        _emailServiceMock.Verify(x => x.SendAsync(
            It.Is<EmailMessage>(message =>
                message.To == "john@example.com" &&
                message.HtmlBody.Contains("confirm-email?token=email-confirmation-token")),
            It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepoMock.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), default), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(default), Times.AtLeast(2));
    }

    [Fact]
    public async Task RegisterAsync_MissingUserName_ThrowsBadRequestException()
    {
        var request = new RegisterRequest
        {
            UserName = " ",
            Email = "john@example.com",
            Password = "Password123!"
        };

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Username is required*");
    }

    [Fact]
    public async Task ForgotPasswordAsync_ExistingUser_SendsFrontendResetLink()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "john@example.com",
            DisplayName = "John",
            IsActive = true
        };
        _userRepoMock.Setup(x => x.GetByEmailAsync("john@example.com", default)).ReturnsAsync(user);
        _jwtTokenGeneratorMock
            .Setup(x => x.GeneratePasswordResetToken(user, It.Is<TimeSpan>(expiry => expiry == TimeSpan.FromHours(1))))
            .Returns("reset-token");

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = " JOHN@example.com " });

        _emailServiceMock.Verify(x => x.SendAsync(
            It.Is<EmailMessage>(message =>
                message.To == user.Email &&
                message.HtmlBody.Contains("https://culinaryblog.example/reset-password?token=reset-token")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndRevokesSessions()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, Email = "john@example.com" };
        _jwtTokenGeneratorMock
            .Setup(x => x.ValidatePasswordResetToken("valid-token", out userId))
            .Returns(true);
        _userRepoMock.Setup(x => x.GetByIdAsync(userId, default)).ReturnsAsync(user);
        _hasherMock.Setup(x => x.HashPassword(user, "NewPassword123!")).Returns("new-hash");

        await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "NewPassword123!"
        });

        user.PasswordHash.Should().Be("new-hash");
        _userRepoMock.Verify(x => x.UpdateAsync(user, default), Times.Once);
        _tokenRepoMock.Verify(x => x.RevokeAllForUserAsync(userId, default), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUserName_ThrowsConflictException()
    {
        // Arrange
        var request = new RegisterRequest { UserName = "existing_user", Email = "test@example.com", Password = "Password123!" };
        _userRepoMock.Setup(x => x.ExistsByUserNameAsync("existing_user", default)).ReturnsAsync(true);

        // Act & Assert
        var act = () => _sut.RegisterAsync(request);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Username*already exists*");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var request = new RegisterRequest { UserName = "new_user", Email = "existing@example.com", Password = "Password123!" };
        _userRepoMock.Setup(x => x.ExistsByUserNameAsync("new_user", default)).ReturnsAsync(false);
        _userRepoMock.Setup(x => x.ExistsByEmailAsync("existing@example.com", default)).ReturnsAsync(true);

        // Act & Assert
        var act = () => _sut.RegisterAsync(request);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Email*already exists*");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "chef_john",
            Email = "john@example.com",
            PasswordHash = "hashed_pwd",
            DisplayName = "Chef John",
            IsActive = true,
            Roles = ["Author"]
        };

        _userRepoMock.Setup(x => x.GetByEmailOrUserNameAsync("chef_john", default)).ReturnsAsync(user);
        _hasherMock.Setup(x => x.VerifyHashedPassword(user, "hashed_pwd", "Password123!"))
            .Returns(PasswordVerificationResult.Success);

        var request = new LoginRequest { UserNameOrEmail = "chef_john", Password = "Password123!" };

        // Act
        var result = await _sut.LoginAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.UserName.Should().Be("chef_john");
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAuthException()
    {
        // Arrange
        _userRepoMock.Setup(x => x.GetByEmailOrUserNameAsync("nonexistent", default))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new LoginRequest { UserNameOrEmail = "nonexistent", Password = "any" };

        // Act & Assert
        var act = () => _sut.LoginAsync(request);
        await act.Should().ThrowAsync<UnauthorizedAuthException>().WithMessage("*Invalid username/email or password*");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedAuthException()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "banned", IsActive = false };
        _userRepoMock.Setup(x => x.GetByEmailOrUserNameAsync("banned", default)).ReturnsAsync(user);

        var request = new LoginRequest { UserNameOrEmail = "banned", Password = "any" };

        // Act & Assert
        var act = () => _sut.LoginAsync(request);
        await act.Should().ThrowAsync<UnauthorizedAuthException>().WithMessage("*Account is inactive*");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAuthException()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "john", PasswordHash = "hashed", IsActive = true };
        _userRepoMock.Setup(x => x.GetByEmailOrUserNameAsync("john", default)).ReturnsAsync(user);
        _hasherMock.Setup(x => x.VerifyHashedPassword(user, "hashed", "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        var request = new LoginRequest { UserNameOrEmail = "john", Password = "wrong" };

        // Act & Assert
        var act = () => _sut.LoginAsync(request);
        await act.Should().ThrowAsync<UnauthorizedAuthException>().WithMessage("*Invalid username/email or password*");
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesTokenAndReturnsNewResponse()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "chef_john",
            Email = "john@example.com",
            DisplayName = "Chef John",
            IsActive = true
        };

        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "somehash",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            RevokedAt = null,
            User = user
        };

        _tokenRepoMock.Setup(x => x.GetByTokenHashWithUserAsync(It.IsAny<string>(), default))
            .ReturnsAsync(storedToken);

        var request = new RefreshRequest { RefreshToken = "raw_valid_refresh_token" };

        // Act
        var result = await _sut.RefreshAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        _tokenRepoMock.Verify(x => x.RevokeAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), default), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(default), Times.AtLeast(2));
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ThrowsUnauthorizedAuthException()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "john" };
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            RevokedAt = DateTime.UtcNow.AddDays(-1),
            User = user
        };

        _tokenRepoMock.Setup(x => x.GetByTokenHashWithUserAsync(It.IsAny<string>(), default))
            .ReturnsAsync(storedToken);

        var request = new RefreshRequest { RefreshToken = "revoked_token" };

        // Act & Assert
        var act = () => _sut.RefreshAsync(request);
        await act.Should().ThrowAsync<UnauthorizedAuthException>().WithMessage("*revoked*");
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ThrowsUnauthorizedAuthException()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "john" };
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            RevokedAt = null,
            User = user
        };

        _tokenRepoMock.Setup(x => x.GetByTokenHashWithUserAsync(It.IsAny<string>(), default))
            .ReturnsAsync(storedToken);

        var request = new RefreshRequest { RefreshToken = "expired_token" };

        // Act & Assert
        var act = () => _sut.RefreshAsync(request);
        await act.Should().ThrowAsync<UnauthorizedAuthException>().WithMessage("*expired*");
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_CallsRevokeAndSaves()
    {
        // Arrange
        var token = "token_to_logout";

        // Act
        await _sut.LogoutAsync(token);

        // Assert
        _tokenRepoMock.Verify(x => x.RevokeAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), null, default), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetMeAsync_UserExists_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var profile = new UserProfileDto(
            userId,
            "john",
            "john@example.com",
            "John Chef",
            null,
            "Bio text",
            true,
            ["User"],
            true,
            DateTime.UtcNow);
        _userRepoMock.Setup(x => x.GetProfileAsync(userId, default)).ReturnsAsync(profile);

        // Act
        var result = await _sut.GetMeAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("john");
        result.DisplayName.Should().Be("John Chef");
    }

    [Fact]
    public async Task UpdateUserRolesAsync_AdminUpdatesTargetAndRevokesRefreshTokens()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = new ApplicationUser { Id = adminId, Roles = ["Admin"] };
        var target = new ApplicationUser { Id = targetId, Email = "98765432@DLU.EDU.VN", Roles = ["Author"] };
        _userRepoMock.Setup(x => x.GetByIdAsync(adminId, default)).ReturnsAsync(admin);
        _userRepoMock.Setup(x => x.GetByIdAsync(targetId, default)).ReturnsAsync(target);

        await _sut.UpdateUserRolesAsync(adminId, targetId, ["Author", "Admin"]);

        target.Roles.Should().Equal("Author", "Admin");
        _userRepoMock.Verify(x => x.UpdateAsync(target, default), Times.Once);
        _tokenRepoMock.Verify(x => x.RevokeAllForUserAsync(targetId, default), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_RejectsAdminRoleForNonDluStudentEmail()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = new ApplicationUser { Id = adminId, Roles = ["Admin"] };
        var target = new ApplicationUser { Id = targetId, Email = "student@example.com", Roles = ["Author"] };
        _userRepoMock.Setup(x => x.GetByIdAsync(adminId, default)).ReturnsAsync(admin);
        _userRepoMock.Setup(x => x.GetByIdAsync(targetId, default)).ReturnsAsync(target);

        var act = () => _sut.UpdateUserRolesAsync(adminId, targetId, ["Author", "Admin"]);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*numeric student ID email at dlu.edu.vn*");
        _userRepoMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>(), default), Times.Never);
        _tokenRepoMock.Verify(x => x.RevokeAllForUserAsync(It.IsAny<Guid>(), default), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_AdminCannotRemoveOwnAdminRole()
    {
        var adminId = Guid.NewGuid();
        var admin = new ApplicationUser { Id = adminId, Roles = ["Admin", "Author"] };
        _userRepoMock.Setup(x => x.GetByIdAsync(adminId, default)).ReturnsAsync(admin);

        var act = () => _sut.UpdateUserRolesAsync(adminId, adminId, ["Author"]);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*cannot remove your own administrator role*");
        _userRepoMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_RejectsUnknownRoles()
    {
        var act = () => _sut.UpdateUserRolesAsync(Guid.NewGuid(), Guid.NewGuid(), ["Superuser"]);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*valid role*");
    }
}
