using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.API.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "AuthPolicy";
    public const string GeneralPolicy = "GeneralPolicy";
    public const string UploadPolicy = "UploadPolicy";

    // Alias giữ tương thích ngược với code hiện tại
    public const string PolicyName = GeneralPolicy;

    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authPermitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var authWindowSeconds = configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60);

        var generalPermitLimit = configuration.GetValue("RateLimiting:General:PermitLimit", 100);
        var generalWindowSeconds = configuration.GetValue("RateLimiting:General:WindowSeconds", 60);

        var uploadPermitLimit = configuration.GetValue("RateLimiting:Upload:PermitLimit", 5);
        var uploadWindowSeconds = configuration.GetValue("RateLimiting:Upload:WindowSeconds", 60);

        var queueLimit = configuration.GetValue("RateLimiting:QueueLimit", 0);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterSpan))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfterSpan.TotalSeconds).ToString();
                }

                var problem = new
                {
                    type = "RATE_LIMIT_EXCEEDED",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Đã vượt quá giới hạn số lượng yêu cầu cho phép. Vui lòng thử lại sau.",
                    instance = context.HttpContext.Request.Path.Value,
                    code = "RATE_LIMIT_EXCEEDED",
                    traceId = context.HttpContext.TraceIdentifier,
                };

                await context.HttpContext.Response.WriteAsJsonAsync(
                    problem,
                    options: (System.Text.Json.JsonSerializerOptions?)null,
                    contentType: "application/problem+json",
                    cancellationToken: cancellationToken);
            };

            // 1. Auth Policy: 10 requests / minute / IP
            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var clientIp = GetClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    clientIp,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromSeconds(authWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimit
                    });
            });

            // 2. General Policy: 100 requests / minute / IP
            options.AddPolicy(GeneralPolicy, httpContext =>
            {
                var clientIp = GetClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    clientIp,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = generalPermitLimit,
                        Window = TimeSpan.FromSeconds(generalWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimit
                    });
            });

            // 3. Upload Policy: 5 requests / minute / IP
            options.AddPolicy(UploadPolicy, httpContext =>
            {
                var clientIp = GetClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    clientIp,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = uploadPermitLimit,
                        Window = TimeSpan.FromSeconds(uploadWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimit
                    });
            });
        });

        return services;
    }

    private static string GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
    }
}
