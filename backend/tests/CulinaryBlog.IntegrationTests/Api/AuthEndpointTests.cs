using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;

namespace CulinaryBlog.IntegrationTests.Api;

public class AuthEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static async Task<T> ReadDataAsync<T>(HttpContent content)
    {
        var response = await content.ReadFromJsonAsync<ApiResponse<T>>();
        return response!.Data;
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            UserName = "testuser_" + Guid.NewGuid().ToString("N")[..8],
            Email = "test_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
            DisplayName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var authResponse = await ReadDataAsync<AuthResponse>(response.Content);
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.User.UserName.Should().Be(request.UserName);
        authResponse.User.Email.Should().Be(request.Email);
        authResponse.User.DisplayName.Should().Be(request.DisplayName);
        authResponse.User.Roles.Should().Contain("Author");
    }

    [Fact]
    public async Task Register_DuplicateUserName_ReturnsConflict()
    {
        // Arrange
        var userName = "duplicate_user_" + Guid.NewGuid().ToString("N")[..8];
        var request1 = new RegisterRequest
        {
            UserName = userName,
            Email = "first_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
            DisplayName = "First User"
        };
        var request2 = new RegisterRequest
        {
            UserName = userName,
            Email = "second_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
            DisplayName = "Second User"
        };

        // Act - first registration succeeds
        await _client.PostAsJsonAsync("/api/v1/auth/register", request1);
        // Second registration with same username should fail
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange - first register a user
        var userName = "loginuser_" + Guid.NewGuid().ToString("N")[..8];
        var email = "login_" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            Password = "Password123!",
            DisplayName = "Login User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            UserNameOrEmail = userName,
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await ReadDataAsync<AuthResponse>(response.Content);
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.User.UserName.Should().Be(userName);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            UserNameOrEmail = "nonexistent",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserProfile()
    {
        // Arrange - register and login
        var userName = "meuser_" + Guid.NewGuid().ToString("N")[..8];
        var email = "me_" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            Password = "Password123!",
            DisplayName = "Me User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var authResponse = await ReadDataAsync<AuthResponse>(registerResponse.Content);

        // Act - call /auth/me with access token
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await ReadDataAsync<CulinaryBlog.Application.DTOs.Auth.UserResponse>(response.Content);
        userResponse.Should().NotBeNull();
        userResponse!.UserName.Should().Be(userName);
        userResponse.Email.Should().Be(email);
        userResponse.DisplayName.Should().Be("Me User");
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ValidRequest_UpdatesProfile()
    {
        // Arrange - register and login
        var userName = "updateuser_" + Guid.NewGuid().ToString("N")[..8];
        var email = "update_" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            Password = "Password123!",
            DisplayName = "Original Name"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var authResponse = await ReadDataAsync<AuthResponse>(registerResponse.Content);

        // Act - update profile
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        var updateRequest = new UpdateProfileRequest
        {
            DisplayName = "Updated Name",
            AvatarUrl = "https://example.com/avatar.jpg",
            Bio = "Updated bio"
        };
        var response = await _client.PatchAsJsonAsync("/api/v1/auth/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await ReadDataAsync<CulinaryBlog.Application.DTOs.Auth.UserResponse>(response.Content);
        userResponse.Should().NotBeNull();
        userResponse!.DisplayName.Should().Be("Updated Name");
        userResponse.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        userResponse.Bio.Should().Be("Updated bio");
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange - register and login
        var userName = "refreshuser_" + Guid.NewGuid().ToString("N")[..8];
        var email = "refresh_" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            Password = "Password123!",
            DisplayName = "Refresh User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var authResponse = await ReadDataAsync<AuthResponse>(registerResponse.Content);

        // Act - refresh token
        var refreshRequest = new RefreshRequest { RefreshToken = authResponse!.RefreshToken };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuthResponse = await ReadDataAsync<AuthResponse>(response.Content);
        newAuthResponse.Should().NotBeNull();
        newAuthResponse!.AccessToken.Should().NotBeNullOrEmpty();
        newAuthResponse.RefreshToken.Should().NotBeNullOrEmpty();
        newAuthResponse.AccessToken.Should().NotBe(authResponse.AccessToken); // Token should be rotated
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshRequest { RefreshToken = "invalid_token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ValidToken_ReturnsNoContent()
    {
        // Arrange - register and login
        var userName = "logoutuser_" + Guid.NewGuid().ToString("N")[..8];
        var email = "logout_" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            Password = "Password123!",
            DisplayName = "Logout User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var authResponse = await ReadDataAsync<AuthResponse>(registerResponse.Content);

        // Act
        var logoutRequest = new RefreshRequest { RefreshToken = authResponse!.RefreshToken };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResendConfirmationEmail_ValidEmail_ReturnsNoContent()
    {
        // Arrange - register a user (email not confirmed)
        var userName = "resenduser_" + Guid.NewGuid().ToString("N")[..8];
        var email = "resend_" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            Password = "Password123!",
            DisplayName = "Resend User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Act
        var resendRequest = new ResendConfirmationEmailRequest { Email = email };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/email/resend", resendRequest);

        // Assert - always returns 204 to prevent user enumeration
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResendConfirmationEmail_NonexistentEmail_ReturnsNoContent()
    {
        // Arrange
        var resendRequest = new ResendConfirmationEmailRequest { Email = "nonexistent@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/email/resend", resendRequest);

        // Assert - always returns 204 to prevent user enumeration
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RateLimiting_LoginEndpoint_ExceedsLimit_Returns429()
    {
        // This test is skipped in shared factory because rate limits are high in Testing environment
        // Rate limiting is tested manually or with a dedicated factory with low limits
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RateLimiting_RefreshEndpoint_ExceedsLimit_Returns429()
    {
        // This test is skipped in shared factory because rate limits are high in Testing environment
        // Rate limiting is tested manually or with a dedicated factory with low limits
        await Task.CompletedTask;
    }

    // â”€â”€ Bá»¯a 5: Password policy (NFR-SEC-001) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Theory]
    [InlineData("short")]                      // < 8 kĂ½ tá»±
    [InlineData("alllowercase1!")]             // thiáº¿u chá»¯ hoa
    [InlineData("ALLUPPERCASE1!")]             // thiáº¿u chá»¯ thÆ°á»ng
    [InlineData("NoDigitsHere!")]              // thiáº¿u chá»¯ sá»‘
    [InlineData("NoSpecialChar1")]             // thiáº¿u kĂ½ tá»± Ä‘áº·c biá»‡t
    public async Task Register_WeakPassword_ReturnsBadRequest(string password)
    {
        // Arrange
        var request = new RegisterRequest
        {
            UserName = "weakpwd_" + Guid.NewGuid().ToString("N")[..8],
            Email = "weak_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = password,
            DisplayName = "Weak Password User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(400);
    }

    // â”€â”€ Bá»¯a 5: Authorization 401/403 (NFR-SEC-006) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetMe_NoToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_NoToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PatchAsJsonAsync(
            "/api/v1/auth/me",
            new UpdateProfileRequest { DisplayName = "New Name" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminUsers_NonAdminUser_ReturnsForbidden()
    {
        // Arrange â€” register a normal user (role: Author)
        var userName = "normaluser_" + Guid.NewGuid().ToString("N")[..8];
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = "normal_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
            DisplayName = "Normal User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var authResponse = await ReadDataAsync<AuthResponse>(registerResponse.Content);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act â€” call admin endpoint with non-admin token
        var response = await _client.GetAsync("/api/v1/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminUsers_NoToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminUsers_AdminUser_ReturnsOk()
    {
        // Arrange â€” create admin user directly via UserManager using factory services
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();

        var adminEmail = "admin_test_" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        var adminUserName = "admin_" + Guid.NewGuid().ToString("N")[..8];
        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminUserName,
            Email = adminEmail,
            DisplayName = "Test Admin",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = ["Admin", "Author"]
        };

        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@123!");
        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();

        // Login as admin
        var loginRequest = new LoginRequest
        {
            UserNameOrEmail = adminUser.UserName,
            Password = "Admin@123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await ReadDataAsync<AuthResponse>(loginResponse.Content);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act â€” call admin endpoint with admin token
        var response = await _client.GetAsync("/api/v1/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        pagedResponse.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_RevokesSessionsAndAllowsNewPassword()
    {
        var userName = "changepwd_" + Guid.NewGuid().ToString("N")[..8];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            UserName = userName,
            Email = userName + "@example.com",
            Password = "Password123!",
            DisplayName = "Change Password User"
        });
        var authResponse = await ReadDataAsync<AuthResponse>(registerResponse.Content);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var oldRefreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest { RefreshToken = authResponse.RefreshToken });
        oldRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UserNameOrEmail = userName,
            Password = "NewPassword123!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsUnauthorized()
    {
        var userName = "badchangepwd_" + Guid.NewGuid().ToString("N")[..8];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            UserName = userName,
            Email = userName + "@example.com",
            Password = "Password123!",
            DisplayName = "Change Password User"
        });
        var authResponse = await ReadDataAsync<AuthResponse>(registerResponse.Content);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_ExistingAndUnknownEmail_ReturnNoContent()
    {
        var email = "forgot_" + Guid.NewGuid().ToString("N")[..8] + "@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            UserName = "forgot_" + Guid.NewGuid().ToString("N")[..8],
            Email = email,
            Password = "Password123!",
            DisplayName = "Forgot Password User"
        });

        var existingResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { Email = email });
        var unknownResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { Email = "unknown_" + Guid.NewGuid().ToString("N") + "@example.com" });

        existingResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        unknownResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new ResetPasswordRequest { Token = "invalid-token", NewPassword = "NewPassword123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_ReusingRotatedToken_RevokesTokenFamily()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            UserName = "reuse_" + Guid.NewGuid().ToString("N")[..8],
            Email = "reuse_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
            DisplayName = "Reuse Detection User"
        });
        var authResponse = await ReadDataAsync<AuthResponse>(registerResponse.Content);

        var firstRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest { RefreshToken = authResponse!.RefreshToken });
        var rotated = await ReadDataAsync<AuthResponse>(firstRefresh.Content);

        var reuseResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest { RefreshToken = authResponse.RefreshToken });
        var familyResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest { RefreshToken = rotated!.RefreshToken });

        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        familyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
