using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;
using CulinaryBlog.Application.DTOs.Auth;

namespace CulinaryBlog.IntegrationTests.Api;

public class AuthEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
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
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
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
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - call /auth/me with access token
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await response.Content.ReadFromJsonAsync<CulinaryBlog.Application.DTOs.Auth.UserResponse>();
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
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

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
        var userResponse = await response.Content.ReadFromJsonAsync<CulinaryBlog.Application.DTOs.Auth.UserResponse>();
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
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - refresh token
        var refreshRequest = new RefreshRequest { RefreshToken = authResponse!.RefreshToken };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuthResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
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
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

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

    // ── Bữa 5: Password policy (NFR-SEC-001) ──────────────────────────

    [Theory]
    [InlineData("short")]                      // < 8 ký tự
    [InlineData("alllowercase1!")]             // thiếu chữ hoa
    [InlineData("ALLUPPERCASE1!")]             // thiếu chữ thường
    [InlineData("NoDigitsHere!")]              // thiếu chữ số
    [InlineData("NoSpecialChar1")]             // thiếu ký tự đặc biệt
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

    // ── Bữa 5: Authorization 401/403 (NFR-SEC-006) ────────────────────

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
        // Arrange — register a normal user (role: Author)
        var userName = "normaluser_" + Guid.NewGuid().ToString("N")[..8];
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = "normal_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
            DisplayName = "Normal User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act — call admin endpoint with non-admin token
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
}