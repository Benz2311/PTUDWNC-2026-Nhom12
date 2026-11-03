using System.Diagnostics;
using System.Security.Claims;
using CulinaryBlog.API.Middleware;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Features.Recipes;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog.Events;
using Xunit;

namespace CulinaryBlog.UnitTests.Infrastructure.Observability;

public sealed class ObservabilityUnitTests
{
    // =========================================================================
    // 1. CORRELATION ID MIDDLEWARE TESTS (Items 1-5, 10, 16)
    // =========================================================================

    [Fact]
    public void ResolveCorrelationId_GeneratesNewGuid_WhenHeaderNotProvided()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var correlationId = CorrelationIdMiddleware.ResolveCorrelationId(context);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _), "Expected a valid GUID string");
    }

    [Fact]
    public void ResolveCorrelationId_ReusesClientHeader_WhenValid()
    {
        // Arrange
        var context = new DefaultHttpContext();
        const string customCorrelationId = "test-correlation-12345_XYZ";
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = customCorrelationId;

        // Act
        var correlationId = CorrelationIdMiddleware.ResolveCorrelationId(context);

        // Assert
        Assert.Equal(customCorrelationId, correlationId);
    }

    [Theory]
    [InlineData("invalid correlation with spaces")]
    [InlineData("xss<script>alert(1)</script>")]
    [InlineData("newline\r\ninjection")]
    [InlineData("toolong_0123456789012345678901234567890123456789012345678901234567890123456789")]
    public void ResolveCorrelationId_RegeneratesSafeGuid_WhenHeaderInvalidOrTooLong(string invalidHeader)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = invalidHeader;

        // Act
        var correlationId = CorrelationIdMiddleware.ResolveCorrelationId(context);

        // Assert
        Assert.NotEqual(invalidHeader, correlationId);
        Assert.True(Guid.TryParse(correlationId, out _), "Invalid or malicious header must be replaced with fresh GUID");
    }

    [Fact]
    public async Task CorrelationIdMiddleware_SetsItem_AndResponseHeader_AndInvokesNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "client-req-999";

        var invoked = false;
        RequestDelegate next = ctx =>
        {
            invoked = true;
            Assert.Equal("client-req-999", ctx.Items[CorrelationIdMiddleware.ItemKey]);
            return Task.CompletedTask;
        };

        var middleware = new CorrelationIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(invoked);
        Assert.Equal("client-req-999", context.Items[CorrelationIdMiddleware.ItemKey]);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_TagsActivity_WhenActivityCurrentExists()
    {
        // Arrange
        var activity = new Activity("TestActivity").Start();
        try
        {
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "trace-corr-456";

            var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("trace-corr-456", activity.GetTagItem("correlation.id"));
            Assert.NotNull(activity.TraceId.ToString());
        }
        finally
        {
            activity.Stop();
            activity.Dispose();
        }
    }

    // =========================================================================
    // 2. USER ID EXTRACTION & ANONYMOUS SAFETY TESTS (Items 9, 10)
    // =========================================================================

    [Fact]
    public void UserIdExtraction_FindsNameIdentifierOrSub_WhenAuthenticated()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-guid-112233"),
                new Claim(ClaimTypes.Email, "chef@culinaryblog.local")
            },
            "TestAuth"));

        // Act
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        // Assert
        Assert.Equal("user-guid-112233", userId);
    }

    [Fact]
    public void UserIdExtraction_FindsSub_WhenNameIdentifierMissing()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "sub-guid-445566")
            },
            "TestAuth"));

        // Act
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        // Assert
        Assert.Equal("sub-guid-445566", userId);
    }

    [Fact]
    public void UserIdExtraction_ReturnsNull_ForAnonymousUser_WithoutCrashing()
    {
        // Arrange
        var anonymousUser = new ClaimsPrincipal();

        // Act
        var userId = anonymousUser.FindFirstValue(ClaimTypes.NameIdentifier) ?? anonymousUser.FindFirstValue("sub");

        // Assert
        Assert.Null(userId);
    }

    // =========================================================================
    // 3. SLOW REQUEST & LOG EVENT LEVEL TESTS (Items 7, 8, 11, 12)
    // =========================================================================

    private static LogEventLevel EvaluateLogLevel(HttpContext httpContext, double elapsedMs, Exception? ex)
    {
        if (ex != null || httpContext.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        if (httpContext.Response.StatusCode >= 400)
        {
            return LogEventLevel.Warning;
        }

        if (elapsedMs > 500)
        {
            return LogEventLevel.Warning;
        }

        return LogEventLevel.Information;
    }

    [Fact]
    public void LogLevelEvaluation_ReturnsWarning_WhenElapsedExceeds500ms()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.StatusCode = 200;

        // Act
        var level = EvaluateLogLevel(context, elapsedMs: 501.0, ex: null);

        // Assert
        Assert.Equal(LogEventLevel.Warning, level);
    }

    [Fact]
    public void LogLevelEvaluation_ReturnsInformation_WhenElapsedIsUnder500ms()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.StatusCode = 200;

        // Act
        var level = EvaluateLogLevel(context, elapsedMs: 499.0, ex: null);

        // Assert
        Assert.Equal(LogEventLevel.Information, level);
    }

    [Fact]
    public void LogLevelEvaluation_ReturnsError_WhenStatusCodeIs500OrExceptionThrown()
    {
        // Arrange
        var context500 = new DefaultHttpContext { Response = { StatusCode = 500 } };
        var contextEx = new DefaultHttpContext { Response = { StatusCode = 200 } };

        // Act
        var level500 = EvaluateLogLevel(context500, elapsedMs: 50.0, ex: null);
        var levelEx = EvaluateLogLevel(contextEx, elapsedMs: 50.0, ex: new InvalidOperationException());

        // Assert
        Assert.Equal(LogEventLevel.Error, level500);
        Assert.Equal(LogEventLevel.Error, levelEx);
    }

    [Fact]
    public void LogLevelEvaluation_ReturnsWarning_WhenStatusCodeIs404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.StatusCode = 404;

        // Act
        var level = EvaluateLogLevel(context, elapsedMs: 30.0, ex: null);

        // Assert
        Assert.Equal(LogEventLevel.Warning, level);
    }

    // =========================================================================
    // 4. OPENTELEMETRY SETUP & TELEMETRY SERVICE TESTS (Items 17-21)
    // =========================================================================

    [Fact]
    public void AddObservability_RegistersTelemetryService_InServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:ServiceName"] = "CulinaryBlog.Test",
                ["OpenTelemetry:Endpoint"] = "http://localhost:4317"
            })
            .Build();

        // Act
        services.AddObservability(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var telemetry = provider.GetService<ICulinaryBlogTelemetry>();
        Assert.NotNull(telemetry);
        Assert.IsType<CulinaryBlogTelemetry>(telemetry);
    }

    [Fact]
    public void CulinaryBlogTelemetry_RecordHttpRequest_DoesNotThrow()
    {
        // Arrange
        using var telemetry = new CulinaryBlogTelemetry();

        // Act & Assert (Không throw exception khi ghi metrics)
        var exception = Record.Exception(() =>
        {
            telemetry.RecordHttpRequest("GET", "/api/v1/recipes", 200, 42.5);
            telemetry.RecordHttpRequest("POST", "/api/v1/recipes", 201, 150.0);
            telemetry.RecordHttpRequest("GET", "/api/v1/recipes/non-existent", 404, 15.0);
            telemetry.RecordHttpRequest("POST", "/api/v1/auth/login", 500, 300.0);
            telemetry.RecordError("DbException", "/api/v1/recipes");
        });

        Assert.Null(exception);
    }

    // =========================================================================
    // 5. BUSINESS METRICS TESTS: RECIPE CREATED & PUBLISHED (Items 22-24)
    // =========================================================================

    [Fact]
    public async Task RecipeWriteService_CreateAsync_CallsRecordRecipeCreated_OnSuccess()
    {
        // Arrange
        var repoMock = new Mock<IRecipeRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var telemetryMock = new Mock<ICulinaryBlogTelemetry>();

        var service = new RecipeWriteService(repoMock.Object, uowMock.Object, telemetryMock.Object);
        var recipe = new Recipe { CategoryId = Guid.NewGuid(), Status = RecipeStatus.Draft };

        // Act
        await service.CreateAsync(recipe);

        // Assert
        repoMock.Verify(r => r.AddAsync(recipe, It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        telemetryMock.Verify(t => t.RecordRecipeCreated(recipe.CategoryId.ToString()), Times.Once);
        telemetryMock.Verify(t => t.RecordRecipePublished(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RecipeWriteService_CreateAsync_CallsBothCreatedAndPublished_WhenInitiallyPublished()
    {
        // Arrange
        var repoMock = new Mock<IRecipeRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var telemetryMock = new Mock<ICulinaryBlogTelemetry>();

        var service = new RecipeWriteService(repoMock.Object, uowMock.Object, telemetryMock.Object);
        var recipe = new Recipe { CategoryId = Guid.NewGuid(), Status = RecipeStatus.Published };

        // Act
        await service.CreateAsync(recipe);

        // Assert
        telemetryMock.Verify(t => t.RecordRecipeCreated(recipe.CategoryId.ToString()), Times.Once);
        telemetryMock.Verify(t => t.RecordRecipePublished(recipe.CategoryId.ToString()), Times.Once);
    }

    [Fact]
    public async Task RecipeWriteService_CreateAsync_DoesNotRecordMetrics_WhenUnitOfWorkFails()
    {
        // Arrange
        var repoMock = new Mock<IRecipeRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var telemetryMock = new Mock<ICulinaryBlogTelemetry>();

        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB Failure"));

        var service = new RecipeWriteService(repoMock.Object, uowMock.Object, telemetryMock.Object);
        var recipe = new Recipe { CategoryId = Guid.NewGuid(), Status = RecipeStatus.Draft };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(recipe));

        telemetryMock.Verify(t => t.RecordRecipeCreated(It.IsAny<string>()), Times.Never);
        telemetryMock.Verify(t => t.RecordRecipePublished(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RecipeWriteService_PublishAsync_UpdatesStatus_AndRecordsMetric_OnSuccess()
    {
        // Arrange
        var repoMock = new Mock<IRecipeRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var telemetryMock = new Mock<ICulinaryBlogTelemetry>();

        var service = new RecipeWriteService(repoMock.Object, uowMock.Object, telemetryMock.Object);
        var recipe = new Recipe { CategoryId = Guid.NewGuid(), Status = RecipeStatus.Draft };

        // Act
        await service.PublishAsync(recipe);

        // Assert
        Assert.Equal(RecipeStatus.Published, recipe.Status);
        Assert.NotNull(recipe.PublishedAt);
        repoMock.Verify(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        telemetryMock.Verify(t => t.RecordRecipePublished(recipe.CategoryId.ToString()), Times.Once);
    }

    [Fact]
    public async Task RecipeWriteService_PublishAsync_DoesNotRecordMetric_WhenUnitOfWorkFails()
    {
        // Arrange
        var repoMock = new Mock<IRecipeRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var telemetryMock = new Mock<ICulinaryBlogTelemetry>();

        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        var service = new RecipeWriteService(repoMock.Object, uowMock.Object, telemetryMock.Object);
        var recipe = new Recipe { CategoryId = Guid.NewGuid(), Status = RecipeStatus.Draft };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAsync(recipe));

        telemetryMock.Verify(t => t.RecordRecipePublished(It.IsAny<string>()), Times.Never);
    }

    // =========================================================================
    // 6. SENSITIVE DATA SECURITY TEST (Item 25)
    // =========================================================================

    [Fact]
    public void CorrelationId_Sanitization_RejectsTokensAndPasswordsInHeader()
    {
        // Arrange
        var sensitiveHeader = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.super_secret_token";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = sensitiveHeader;

        // Act
        var result = CorrelationIdMiddleware.ResolveCorrelationId(context);

        // Assert: Chuỗi nhạy cảm có chứa ký tự space, dot,... sẽ bị reject và thay bằng GUID an toàn
        Assert.NotEqual(sensitiveHeader, result);
        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("Bearer", result);
    }
}
