using System.Text.Json;
using CulinaryBlog.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Middleware;

/// <summary>
/// Middleware xử lý ngoại lệ tập trung (Global Exception Handling) theo chuẩn RFC 7807 Problem Details.
/// Đảm bảo:
/// 1. Ánh xạ StorageUnavailableException thành HTTP 503 Service Unavailable.
/// 2. Ánh xạ StorageException thành HTTP 500.
/// 3. Ánh xạ NotFoundException thành HTTP 404 Not Found.
/// 4. Ánh xạ ForbiddenException thành HTTP 403 Forbidden.
/// 5. Ánh xạ ValidationException thành HTTP 422 Unprocessable Entity.
/// 6. Ánh xạ ArgumentException thành HTTP 400 Bad Request.
/// 7. Tuyệt đối không rò rỉ credential, secret key, hoặc stack trace nội bộ ra client.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Đã xảy ra ngoại lệ: {Message}", exception.Message);

        var (statusCode, problemDetails) = exception switch
        {
            StorageUnavailableException storageEx => (
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Service Unavailable",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                    Detail = storageEx.Message,
                }),

            StorageException => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Storage Error",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Detail = "Đã xảy ra sự cố trong quá trình lưu trữ tệp tin.",
                }),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Detail = notFoundEx.Message,
                }),

            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Detail = forbiddenEx.Message,
                }),

            ValidationException validationEx => (
                StatusCodes.Status422UnprocessableEntity,
                (ProblemDetails)new ValidationProblemDetails(validationEx.Errors)
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Validation Failed",
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Detail = validationEx.Message,
                }),

            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Bad Request",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = argEx.Message,
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Detail = "Đã xảy ra lỗi nội bộ máy chủ. Vui lòng liên hệ quản trị viên.",
                }),
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
        });

        await context.Response.WriteAsync(json);
    }
}
