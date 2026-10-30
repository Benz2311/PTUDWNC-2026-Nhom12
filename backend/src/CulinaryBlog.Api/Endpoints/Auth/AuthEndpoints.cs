using System.Security.Claims;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

        // POST /api/v1/auth/register
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            logger.LogInformation("Registration attempt for: {UserName} ({Email}) from IP: {IP}",
                request.UserName, request.Email, ipAddress);

            var result = await authService.RegisterAsync(request, ipAddress);
            logger.LogInformation("Registration successful for: {UserName}", request.UserName);
            return Results.Created($"/api/v1/auth/me", ApiResponse.Success(result));
        })
        .WithName("Register")
        .WithSummary("Đăng ký tài khoản tác giả")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // POST /api/v1/auth/login
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            logger.LogInformation("Login attempt for: {Login} from IP: {IP}",
                request.UserNameOrEmail, ipAddress);

            var result = await authService.LoginAsync(request, ipAddress);
            logger.LogInformation("Login successful for: {Login}", request.UserNameOrEmail);
            return Results.Ok(ApiResponse.Success(result));
        })
        .WithName("Login")
        .WithSummary("Đăng nhập tài khoản")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /api/v1/auth/google
        group.MapPost("/google", async (
            [FromBody] GoogleLoginRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            logger.LogInformation("Google login attempt from IP: {IP}", ipAddress);

            var result = await authService.GoogleLoginAsync(request, ipAddress);
            logger.LogInformation("Google login successful");
            return Results.Ok(ApiResponse.Success(result));
        })
        .WithName("GoogleLogin")
        .WithSummary("Đăng nhập bằng tài khoản Google")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /api/v1/auth/refresh
        group.MapPost("/refresh", async (
            [FromBody] RefreshRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            logger.LogDebug("Token refresh attempt from IP: {IP}", ipAddress);

            var result = await authService.RefreshAsync(request, ipAddress);
            logger.LogDebug("Token refresh successful");
            return Results.Ok(ApiResponse.Success(result));
        })
        .WithName("RefreshToken")
        .WithSummary("Làm mới Access Token bằng Refresh Token")
        .Produces<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /api/v1/auth/refresh-token (legacy compatibility)
        group.MapPost("/refresh-token", async (
            [FromBody] RefreshRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            logger.LogDebug("Token refresh (legacy) attempt from IP: {IP}", ipAddress);

            var result = await authService.RefreshAsync(request, ipAddress);
            logger.LogDebug("Token refresh (legacy) successful");
            return Results.Ok(ApiResponse.Success(result));
        })
        .ExcludeFromDescription();

        // POST /api/v1/auth/logout
        group.MapPost("/logout", async (
            [FromBody] RefreshRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            logger.LogInformation("Logout attempt from IP: {IP}", ipAddress);

            await authService.LogoutAsync(request.RefreshToken);
            logger.LogInformation("Logout successful");
            return Results.NoContent();
        })
        .WithName("Logout")
        .WithSummary("Đăng xuất tài khoản")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /api/v1/auth/email/confirm
        group.MapPost("/email/confirm", async (
            [FromBody] ConfirmEmailRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            logger.LogInformation("Email confirmation attempt");

            await authService.ConfirmEmailAsync(request);
            logger.LogInformation("Email confirmation successful");
            return Results.NoContent();
        })
        .WithName("ConfirmEmail")
        .WithSummary("Xác nhận địa chỉ email")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // POST /api/v1/auth/email/resend
        group.MapPost("/email/resend", async (
            [FromBody] ResendConfirmationEmailRequest request,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            logger.LogInformation("Resend confirmation email for: {Email}", request.Email);

            await authService.ResendConfirmationEmailAsync(request);
            logger.LogInformation("Resend confirmation email processed for: {Email}", request.Email);
            return Results.NoContent();
        })
        .WithName("ResendConfirmationEmail")
        .WithSummary("Gửi lại email xác nhận")
        .Produces(StatusCodes.Status204NoContent);

        // GET /api/v1/auth/me
        group.MapGet("/me", async (
            ClaimsPrincipal userPrincipal,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var userIdClaim = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? userPrincipal.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                logger.LogWarning("GetMe: No user ID claim found");
                return Results.Unauthorized();
            }

            logger.LogDebug("GetMe request for user: {UserId}", userIdClaim);

            var user = await authService.GetMeAsync(userIdClaim);
            return user is null
                ? Results.NotFound()
                : Results.Ok(ApiResponse.Success(user));
        })
        .RequireAuthorization()
        .WithName("GetMe")
        .WithSummary("Lấy thông tin người dùng đang đăng nhập")
        .Produces<ApiResponse<UserResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // PATCH /api/v1/auth/me
        group.MapPatch("/me", async (
            [FromBody] UpdateProfileRequest request,
            ClaimsPrincipal userPrincipal,
            IAuthService authService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var userIdClaim = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? userPrincipal.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                logger.LogWarning("UpdateProfile: No user ID claim found");
                return Results.Unauthorized();
            }

            logger.LogInformation("UpdateProfile request for user: {UserId}", userIdClaim);

            var result = await authService.UpdateProfileAsync(userIdClaim, request);
            logger.LogInformation("UpdateProfile successful for user: {UserId}", userIdClaim);
            return Results.Ok(ApiResponse.Success(result));
        })
        .RequireAuthorization()
        .WithName("UpdateProfile")
        .WithSummary("Cập nhật thông tin cá nhân")
        .Produces<ApiResponse<UserResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // POST /api/v1/auth/change-password
        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            ClaimsPrincipal userPrincipal,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? userPrincipal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            await authService.ChangePasswordAsync(userId, request);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ChangePassword")
        .WithSummary("Đổi mật khẩu")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /api/v1/auth/forgot-password
        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.ForgotPasswordAsync(request);
            return Results.NoContent();
        })
        .WithName("ForgotPassword")
        .WithSummary("Yêu cầu quên mật khẩu")
        .Produces(StatusCodes.Status204NoContent);

        // POST /api/v1/auth/reset-password
        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.ResetPasswordAsync(request);
            return Results.NoContent();
        })
        .WithName("ResetPassword")
        .WithSummary("Đặt lại mật khẩu")
        .Produces(StatusCodes.Status204NoContent);
    }
}
