using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Extensions;

public static class RateLimitingExtensions
{
    public const string PolicyName = "GeneralPolicy";

    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 60);
        var windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 60);
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
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Đã vượt quá giới hạn số lượng yêu cầu cho phép. Vui lòng thử lại sau.",
                    instance = context.HttpContext.Request.Path.Value,
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
            };

            // Policy giới hạn request cho các API thông thường (GeneralPolicy)
            options.AddFixedWindowLimiter(
                policyName: PolicyName,
                limiterOptions =>
                {
                    limiterOptions.PermitLimit = permitLimit;
                    limiterOptions.Window = TimeSpan.FromSeconds(windowSeconds);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = queueLimit;
                });
        });

        return services;
    }
}
