using System.Diagnostics;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.Api.Middleware;

/// <summary>
/// Global Exception Handler xử lý lỗi toàn cục và trả về RFC 7807 Problem Details (Lab 3)
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, type, detail) = MapException(exception);

        _logger.LogError(exception, "Global Exception Handler caught an unhandled exception: {Message}. Status: {StatusCode}", exception.Message, statusCode);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // Bổ sung TraceIdentifier để hỗ trợ tracing/observability
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        // Nếu là ValidationException, đưa danh sách lỗi chi tiết vào extensions
        if (exception is ValidationException validationEx && validationEx.Errors.Any())
        {
            problemDetails.Extensions["errors"] = validationEx.Errors;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json; charset=utf-8",
            cancellationToken: cancellationToken);

        return true;
    }

    private (int StatusCode, string Title, string Type, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                notFound.Message
            ),

            ForbiddenException forbidden => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                forbidden.Message
            ),

            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                validation.Message
            ),

            DomainException domain => (
                StatusCodes.Status400BadRequest,
                "Domain Business Rule Violation",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                domain.Message
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                // Tuyệt đối không để lộ stack trace/internal SQL error cho client (Lab 3 requirement)
                _env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."
            )
        };
    }
}
