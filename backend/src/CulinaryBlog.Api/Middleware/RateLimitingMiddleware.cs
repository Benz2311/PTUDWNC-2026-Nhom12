using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Api.Middleware;

public sealed class RateLimitingOptions
{
    public int PermitLimit { get; set; } = 100;
    public int WindowMinutes { get; set; } = 1;
    public string PolicyName { get; set; } = "default";
    public string[]? Paths { get; set; }
    public string[]? Methods { get; set; }
}

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this middleware should apply to this path
        if (_options.Paths != null && _options.Paths.Length > 0)
        {
            var path = context.Request.Path.Value ?? "";
            var shouldApply = _options.Paths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (!shouldApply)
            {
                await _next(context);
                return;
            }
        }

        // Optionally restrict to specific HTTP methods (e.g. upload = POST only)
        if (_options.Methods != null && _options.Methods.Length > 0)
        {
            var method = context.Request.Method;
            if (!_options.Methods.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"ratelimit:{_options.PolicyName}:{ipAddress}";

        var currentCount = _cache.Get<int>(cacheKey);
        if (currentCount >= _options.PermitLimit)
        {
            _logger.LogWarning("Rate limit exceeded for IP {IP} on policy {Policy}", ipAddress, _options.PolicyName);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers["Retry-After"] = (_options.WindowMinutes * 60).ToString();
            context.Response.Headers["X-RateLimit-Limit"] = _options.PermitLimit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";

            // RFC 7807 Problem Details — no sensitive information disclosed
            var problem = new
            {
                type = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = (int)HttpStatusCode.TooManyRequests,
                detail = "Rate limit exceeded. Please retry after the specified interval.",
                instance = context.Request.Path.Value,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        _cache.Set(cacheKey, currentCount + 1, TimeSpan.FromMinutes(_options.WindowMinutes));

        context.Response.Headers["X-RateLimit-Limit"] = _options.PermitLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, _options.PermitLimit - currentCount - 1).ToString();

        await _next(context);
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimitingPolicy(
        this IApplicationBuilder app,
        string policyName,
        int permitLimit,
        int windowMinutes,
        string[] paths,
        string[]? methods = null)
    {
        return app.UseMiddleware<RateLimitingMiddleware>(Options.Create(new RateLimitingOptions
        {
            PolicyName = policyName,
            PermitLimit = permitLimit,
            WindowMinutes = windowMinutes,
            Paths = paths,
            Methods = methods
        }));
    }
}