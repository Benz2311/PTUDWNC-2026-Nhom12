using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    private const int AccessTokenLifetimeMinutes = 15;
    private const int RefreshTokenLifetimeDays = 7;
    private static readonly PasswordHasher<ApplicationUser> PasswordHasher = new();

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", Register).RequireRateLimiting("auth");
        app.MapPost("/api/v1/auth/register", Register).RequireRateLimiting("auth");
        app.MapPost("/api/auth/login", Login).RequireRateLimiting("auth");
        app.MapPost("/api/v1/auth/login", Login).RequireRateLimiting("auth");
        app.MapPost("/api/auth/refresh", Refresh).RequireRateLimiting("auth");
        app.MapPost("/api/v1/auth/refresh", Refresh).RequireRateLimiting("auth");
        app.MapPost("/api/auth/logout", Logout).RequireAuthorization();
        app.MapPost("/api/v1/auth/logout", Logout).RequireAuthorization();
        app.MapGet("/api/auth/me", GetProfile).RequireAuthorization();
        app.MapGet("/api/v1/auth/me", GetProfile).RequireAuthorization();
        app.MapPatch("/api/auth/me", UpdateProfile).RequireAuthorization();
        app.MapPatch("/api/v1/auth/me", UpdateProfile).RequireAuthorization();
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        ApplicationDbContext db,
        IAuthenticationService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Full name, email and password are required." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return Results.Conflict(new { error = "Email already exists." });
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            UserName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim(),
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            Roles = ["Author"]
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var tokens = await IssueTokensAsync(user, authService, db, httpContext, cancellationToken);
        return Results.Created("/api/v1/auth/me", ToAuthResponse(user, tokens));
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        ApplicationDbContext db,
        IAuthenticationService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.PasswordHash) ||
            PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        var tokens = await IssueTokensAsync(user, authService, db, httpContext, cancellationToken);
        return Results.Ok(ToAuthResponse(user, tokens));
    }

    private static async Task<IResult> Refresh(
        RefreshRequest request,
        ApplicationDbContext db,
        IAuthenticationService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.Unauthorized();
        }

        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await db.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.Token == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return Results.Unauthorized();
        }

        if (storedToken.IsRevoked)
        {
            var activeTokens = await db.RefreshTokens
                .Where(token => token.UserId == storedToken.UserId && !token.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var activeToken in activeTokens)
            {
                activeToken.IsRevoked = true;
                activeToken.RevokedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Unauthorized();
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow || !storedToken.User.IsActive)
        {
            return Results.Unauthorized();
        }

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        var tokens = await IssueTokensAsync(storedToken.User, authService, db, httpContext, cancellationToken);
        storedToken.ReplacedByToken = tokens.RefreshTokenHash;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToAuthResponse(storedToken.User, tokens));
    }

    private static async Task<IResult> Logout(
        RefreshRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.NoContent();
        }

        var userId = GetUserId(principal);
        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await db.RefreshTokens.SingleOrDefaultAsync(token => token.Token == tokenHash && token.UserId == userId, cancellationToken);
        if (storedToken is not null && !storedToken.IsRevoked)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetProfile(ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user is null ? Results.NotFound() : Results.Ok(ToProfile(user));
    }

    private static async Task<IResult> UpdateProfile(ProfileRequest request, ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        if (request.FullName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 100)
            {
                return Results.UnprocessableEntity(new { error = "FullName must be between 1 and 100 characters." });
            }

            user.FullName = request.FullName.Trim();
        }

        if (request.AvatarUrl is not null)
        {
            if (!Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out var avatarUri) || avatarUri.Scheme is not ("http" or "https"))
            {
                return Results.UnprocessableEntity(new { error = "AvatarUrl must be a valid HTTP or HTTPS URL." });
            }

            user.AvatarUrl = request.AvatarUrl.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToProfile(user));
    }

    private static async Task<IssuedTokens> IssueTokensAsync(ApplicationUser user, IAuthenticationService authService, ApplicationDbContext db, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var accessToken = await authService.GenerateAccessTokenAsync(user.Id, user.Roles, cancellationToken);
        var refreshToken = await authService.GenerateRefreshTokenAsync(user.Id, cancellationToken);
        var refreshTokenHash = HashToken(refreshToken);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays),
            CreatedByIp = httpContext.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync(cancellationToken);
        return new IssuedTokens(accessToken, refreshToken, refreshTokenHash, DateTime.UtcNow.AddMinutes(AccessTokenLifetimeMinutes));
    }

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)
            ? userId
            : Guid.Empty;

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static object ToAuthResponse(ApplicationUser user, IssuedTokens tokens) => new
    {
        accessToken = tokens.AccessToken,
        refreshToken = tokens.RefreshToken,
        expiresAt = tokens.ExpiresAt,
        user = ToProfile(user)
    };

    private static object ToProfile(ApplicationUser user) => new
    {
        id = user.Id,
        fullName = user.FullName,
        userName = user.UserName,
        email = user.Email,
        avatarUrl = user.AvatarUrl,
        roles = user.Roles,
        emailConfirmed = user.EmailConfirmed,
        createdAt = user.CreatedAt
    };

    private sealed record IssuedTokens(string AccessToken, string RefreshToken, string RefreshTokenHash, DateTime ExpiresAt);
}

public sealed record RegisterRequest(string FullName, string Email, string? UserName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ProfileRequest(string? FullName, string? AvatarUrl);