using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — ghi structured log cho mọi Command/Query:
/// - Log bắt đầu xử lý với metadata (RequestName)
/// - Đo thời gian xử lý (ElapsedMilliseconds)
/// - Cảnh báo Warning nếu thời gian xử lý vượt quá 500ms (Slow Request)
/// - Log Error nếu có Exception
/// - Bảo mật: Tuyệt đối KHÔNG log body/params/properties tránh rò rỉ token, password, PII.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling MediatR request {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (elapsedMs > 500)
            {
                _logger.LogWarning(
                    "Long-running MediatR request {RequestName} completed in {ElapsedMilliseconds} ms (> 500ms)",
                    requestName,
                    elapsedMs);
            }
            else
            {
                _logger.LogInformation(
                    "Handled MediatR request {RequestName} in {ElapsedMilliseconds} ms",
                    requestName,
                    elapsedMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Error handling MediatR request {RequestName} after {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
