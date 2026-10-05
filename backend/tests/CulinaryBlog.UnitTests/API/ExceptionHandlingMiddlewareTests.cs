using System.Text.Json;
using CulinaryBlog.API.Middleware;
using CulinaryBlog.Application.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CulinaryBlog.UnitTests.API;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_WhenStorageUnavailableExceptionThrown_Returns503ProblemDetails()
    {
        // Arrange
        RequestDelegate next = _ => throw new StorageUnavailableException("MinIO is down");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.ContentType.Should().Be("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(503);
        problemDetails.Title.Should().Be("Service Unavailable");
        problemDetails.Detail.Should().Be("MinIO is down");
        responseBody.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationExceptionThrown_Returns422ProblemDetails()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["file"] = ["File vượt quá dung lượng cho phép."]
        };
        RequestDelegate next = _ => throw new ValidationException(errors);
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("file");
        problemDetails.Errors["file"].Should().Contain("File vượt quá dung lượng cho phép.");
        responseBody.Should().Contain("File vượt quá dung lượng cho phép.");
    }

    [Fact]
    public async Task InvokeAsync_WhenNotFoundExceptionThrown_Returns404ProblemDetails()
    {
        // Arrange
        RequestDelegate next = _ => throw new NotFoundException("Recipe not found");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
