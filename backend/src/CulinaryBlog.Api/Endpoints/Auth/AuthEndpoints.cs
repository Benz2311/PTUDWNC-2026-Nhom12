using System.Security.Claims;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        MapRoutes(app.MapGroup("/api/v1/auth")
            .WithTags("Auth"),
            RateLimitingExtensions.AuthPolicy,
            RateLimitingExtensions.GeneralPolicy);
        MapRoutes(app.MapGroup("/api/auth")
            .WithTags("Auth"),
            "auth",
            "api");
    }

    private static void MapRoutes(RouteGroupBuilder group, string authPolicy, string generalPolicy)
    {
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IAuthService authService,
            HttpContext httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var result = await authService.RegisterAsync(request, ipAddress);
            return Results.Created("/api/v1/auth/me", ApiResponse.Success(result));
        }).RequireRateLimiting(authPolicy);

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuthService authService,
            HttpContext httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var result = await authService.LoginAsync(request, ipAddress);
            return Results.Ok(ApiResponse.Success(result));
        }).RequireRateLimiting(authPolicy);

        group.MapPost("/google", async (
            [FromBody] GoogleLoginRequest request,
            IAuthService authService,
            HttpContext httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var result = await authService.GoogleLoginAsync(request, ipAddress);
            return Results.Ok(ApiResponse.Success(result));
        }).RequireRateLimiting(authPolicy);

        group.MapPost("/refresh", Refresh).RequireRateLimiting(authPolicy);
        group.MapPost("/refresh-token", Refresh).ExcludeFromDescription().RequireRateLimiting(authPolicy);
        group.MapPost("/logout", async ([FromBody] RefreshRequest request, IAuthService authService) =>
        {
            await authService.LogoutAsync(request.RefreshToken);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting(authPolicy);

        group.MapPost("/email/confirm", async (
            [FromBody] ConfirmEmailRequest request,
            IAuthService authService) =>
        {
            await authService.ConfirmEmailAsync(request);
            return Results.NoContent();
        }).RequireRateLimiting(authPolicy);
        group.MapPost("/email/resend", async (
            [FromBody] ResendConfirmationEmailRequest request,
            IAuthService authService) =>
        {
            await authService.ResendConfirmationEmailAsync(request);
            return Results.NoContent();
        }).RequireRateLimiting(authPolicy);

        group.MapGet("/me", async (ClaimsPrincipal principal, IAuthService authService) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetMeAsync(userId);
            return user is null
                ? Results.NotFound()
                : Results.Ok(ApiResponse.Success(user));
        }).RequireAuthorization().RequireRateLimiting(generalPolicy);

        group.MapPatch("/me", async (
            [FromBody] UpdateProfileRequest request,
            ClaimsPrincipal principal,
            IAuthService authService) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var user = await authService.UpdateProfileAsync(userId, request);
            return Results.Ok(ApiResponse.Success(user));
        }).RequireAuthorization().RequireRateLimiting(generalPolicy);

        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            ClaimsPrincipal principal,
            IAuthService authService) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            await authService.ChangePasswordAsync(userId, request);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting(authPolicy);

        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            IAuthService authService) =>
        {
            await authService.ForgotPasswordAsync(request);
            return Results.NoContent();
        }).RequireRateLimiting(authPolicy);
        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            IAuthService authService) =>
        {
            await authService.ResetPasswordAsync(request);
            return Results.NoContent();
        }).RequireRateLimiting(authPolicy);
    }

    private static async Task<IResult> Refresh(
        [FromBody] RefreshRequest request,
        IAuthService authService,
        HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await authService.RefreshAsync(request, ipAddress);
        return Results.Ok(ApiResponse.Success(result));
    }
}
