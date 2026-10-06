using System.Security.Claims;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Registration attempt for: {UserName} ({Email}) from IP: {IP}", 
            request.UserName, request.Email, ipAddress);
        
        var result = await _authService.RegisterAsync(request, ipAddress);
        _logger.LogInformation("Registration successful for: {UserName}", request.UserName);
        return CreatedAtAction(nameof(Register), result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Login attempt for: {Login} from IP: {IP}", 
            request.UserNameOrEmail, ipAddress);
        
        var result = await _authService.LoginAsync(request, ipAddress);
        _logger.LogInformation("Login successful for: {Login}", request.UserNameOrEmail);
        return Ok(result);
    }

    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Google login attempt from IP: {IP}", ipAddress);
        
        var result = await _authService.GoogleLoginAsync(request, ipAddress);
        _logger.LogInformation("Google login successful");
        return Ok(result);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogDebug("Token refresh attempt from IP: {IP}", ipAddress);
        
        var result = await _authService.RefreshAsync(request, ipAddress);
        _logger.LogDebug("Token refresh successful");
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Obsolete("Use /auth/refresh instead")]
    public async Task<IActionResult> RefreshTokenLegacy(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogDebug("Token refresh (legacy) attempt from IP: {IP}", ipAddress);
        
        var result = await _authService.RefreshAsync(request, ipAddress);
        _logger.LogDebug("Token refresh (legacy) successful");
        return Ok(result);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Logout attempt from IP: {IP}", ipAddress);
        
        await _authService.LogoutAsync(request.RefreshToken);
        _logger.LogInformation("Logout successful");
        return NoContent();
    }

    [HttpPost("email/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email confirmation attempt");
        
        await _authService.ConfirmEmailAsync(request);
        _logger.LogInformation("Email confirmation successful");
        return NoContent();
    }

    [HttpPost("email/resend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendConfirmationEmail(
        [FromBody] ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resend confirmation email for: {Email}", request.Email);
        
        await _authService.ResendConfirmationEmailAsync(request);
        _logger.LogInformation("Resend confirmation email processed for: {Email}", request.Email);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            _logger.LogWarning("GetMe: No user ID claim found");
            return Unauthorized();
        }

        _logger.LogDebug("GetMe request for user: {UserId}", userIdClaim);
        
        var user = await _authService.GetMeAsync(userIdClaim);
        return user is null
            ? NotFound()
            : Ok(user);
    }

    [Authorize]
    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            _logger.LogWarning("UpdateProfile: No user ID claim found");
            return Unauthorized();
        }

        _logger.LogInformation("UpdateProfile request for user: {UserId}", userIdClaim);
        
        var result = await _authService.UpdateProfileAsync(userIdClaim, request);
        _logger.LogInformation("UpdateProfile successful for user: {UserId}", userIdClaim);
        return Ok(result);
    }
}
