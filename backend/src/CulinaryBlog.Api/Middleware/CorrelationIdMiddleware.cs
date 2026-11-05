using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog.Context;

namespace CulinaryBlog.API.Middleware;

/// <summary>
/// Middleware quản lý CorrelationId cho mỗi request:
/// 1. Kiểm tra header X-Correlation-ID từ client.
/// 2. Nếu hợp lệ (an toàn, độ dài <= 64), tái sử dụng; ngược lại tự sinh GUID mới.
/// 3. Đưa CorrelationId vào HttpContext.Items, Response Headers, và Serilog LogContext.
/// 4. Liên kết với OpenTelemetry Activity (TraceId / SpanId) nếu có.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";
    private const int MaxCorrelationIdLength = 64;

    private static readonly Regex SafeCorrelationIdRegex =
        new(@"^[a-zA-Z0-9_\-]+$", RegexOptions.Compiled);

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values) &&
            !string.IsNullOrWhiteSpace(values))
        {
            var candidate = values.ToString().Trim();
            if (candidate.Length <= MaxCorrelationIdLength && SafeCorrelationIdRegex.IsMatch(candidate))
            {
                return candidate;
            }
        }

        return Guid.NewGuid().ToString("D");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        // Lưu vào context items để endpoint/controller/service truy cập
        context.Items[ItemKey] = correlationId;

        // Trả về header cho client
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderName))
            {
                context.Response.Headers.Append(HeaderName, correlationId);
            }

            return Task.CompletedTask;
        });

        // Gắn tag vào OpenTelemetry Activity nếu có
        var currentActivity = Activity.Current;
        currentActivity?.SetTag("correlation.id", correlationId);

        // Đẩy vào LogContext của Serilog
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            if (currentActivity != null)
            {
                using (LogContext.PushProperty("TraceId", currentActivity.TraceId.ToString()))
                using (LogContext.PushProperty("SpanId", currentActivity.SpanId.ToString()))
                {
                    await _next(context);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
