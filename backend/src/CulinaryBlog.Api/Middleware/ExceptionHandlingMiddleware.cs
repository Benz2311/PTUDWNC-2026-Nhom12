using System.Net;
using System.Text.Json;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApplicationNotFoundException = CulinaryBlog.Application.Exceptions.NotFoundException;
using ApplicationValidationException = CulinaryBlog.Application.Exceptions.ValidationException;

namespace CulinaryBlog.Api.Middleware;

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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var path = context.Request.Path;
        var method = context.Request.Method;

        // Log the exception with context
        _logger.LogError(exception, 
            "Unhandled exception: {Method} {Path} - TraceId: {TraceId}, ExceptionType: {ExceptionType}, Message: {Message}",
            method, path, traceId, exception.GetType().Name, exception.Message);

        context.Response.ContentType = "application/problem+json";

        switch (exception)
        {
            // Domain exceptions (RFC 7807): ConcurrencyConflictException → 409,
            // NotFoundException → 404, UnauthorizedAuthException → 401, ConflictException → 409
            case BaseDomainException domainException:
                _logger.LogWarning(
                    "Domain exception: {Method} {Path} - TraceId: {TraceId}, StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, Message: {Message}",
                    method, path, traceId, domainException.StatusCode, domainException.ErrorCode, domainException.Message);
                
                context.Response.StatusCode = (int)domainException.StatusCode;
                return WriteProblemDetailsAsync(
                    context,
                    domainException.StatusCode,
                    domainException.ErrorCode,
                    domainException.Message,
                    path,
                    traceId);

            // FluentValidation errors (MediatR ValidationBehavior) → 400
            case FluentValidation.ValidationException fluentValidationException:
                _logger.LogWarning(
                    "Validation error: {Method} {Path} - TraceId: {TraceId}, Errors: {Errors}",
                    method, path, traceId, JsonSerializer.Serialize(fluentValidationException.Errors));
                
                return WriteValidationProblemAsync(
                    context,
                    ToErrorDictionary(fluentValidationException),
                    path,
                    traceId);

            // Application-layer validation errors → 400
            case ApplicationValidationException applicationValidationException:
                _logger.LogWarning(
                    "Application validation error: {Method} {Path} - TraceId: {TraceId}, Errors: {Errors}",
                    method, path, traceId, JsonSerializer.Serialize(applicationValidationException.Errors));
                
                return WriteValidationProblemAsync(
                    context,
                    applicationValidationException.Errors,
                    path,
                    traceId);

            // Application-layer not found / forbidden → 404 / 403
            case ApplicationNotFoundException applicationNotFoundException:
                _logger.LogWarning(
                    "Not found: {Method} {Path} - TraceId: {TraceId}, Message: {Message}",
                    method, path, traceId, applicationNotFoundException.Message);
                
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return WriteProblemDetailsAsync(
                    context,
                    HttpStatusCode.NotFound,
                    "NOT_FOUND",
                    applicationNotFoundException.Message,
                    path,
                    traceId);

            // Authentication failures (safety net) → 401
            case UnauthorizedAccessException unauthorizedException:
                _logger.LogWarning(
                    "Unauthorized access: {Method} {Path} - TraceId: {TraceId}, Message: {Message}",
                    method, path, traceId, unauthorizedException.Message);
                
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return WriteProblemDetailsAsync(
                    context,
                    HttpStatusCode.Unauthorized,
                    "UNAUTHORIZED",
                    unauthorizedException.Message,
                    path,
                    traceId);

            // Model binding / bad request → 400
            case BadHttpRequestException badHttpRequestException:
                return HandleBadRequestExceptionAsync(context, badHttpRequestException, path, traceId);

            default:
                _logger.LogError(exception, 
                    "Internal server error: {Method} {Path} - TraceId: {TraceId}",
                    method, path, traceId);
                
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return WriteProblemDetailsAsync(
                    context,
                    HttpStatusCode.InternalServerError,
                    "INTERNAL_SERVER_ERROR",
                    "An internal server error occurred.",
                    path,
                    traceId);
        }
    }

    private Task HandleBadRequestExceptionAsync(
        HttpContext context,
        BadHttpRequestException exception,
        string path,
        string traceId)
    {
        var errors = exception.InnerException is ApplicationValidationException validationException
            ? validationException.Errors
            : new Dictionary<string, string[]> { ["General"] = [exception.Message] };

        _logger.LogWarning(
            "Bad request: {Method} {Path} - TraceId: {TraceId}, Message: {Message}",
            context.Request.Method, path, traceId, exception.Message);

        return WriteValidationProblemAsync(context, errors, path, traceId);
    }

    private static Dictionary<string, string[]> ToErrorDictionary(FluentValidation.ValidationException exception)
    {
        return exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());
    }

    private Task WriteValidationProblemAsync(
        HttpContext context,
        IDictionary<string, string[]> errors,
        string path,
        string traceId)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Type = "https://httpstatuses.com/400",
            Title = "VALIDATION_ERROR",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = path,
        };

        problemDetails.Extensions["traceId"] = traceId;

        return WriteJsonAsync(context, problemDetails);
    }

    private static Task WriteProblemDetailsAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail,
        string instance,
        string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = instance,
        };

        problemDetails.Extensions["traceId"] = traceId;

        return WriteJsonAsync(context, problemDetails);
    }

    private static Task WriteJsonAsync(HttpContext context, object value)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(value, options));
    }
}
