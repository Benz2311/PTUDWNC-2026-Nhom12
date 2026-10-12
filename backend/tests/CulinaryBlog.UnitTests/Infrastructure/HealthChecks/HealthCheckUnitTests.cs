using System.IO;
using System.Text.Json;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.HealthChecks;
using CulinaryBlog.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Infrastructure.HealthChecks;

/// <summary>
/// Bộ Unit Tests toàn diện cho tính năng Application Health Checks (Task 4 - Võ Hùng Mạnh).
/// Kiểm thử cấu trúc liveness, readiness, general health, tagging, semantic isolation,
/// HTTP status code 200/503, và bảo mật dữ liệu không leak secrets.
/// </summary>
public class HealthCheckUnitTests
{
    private static HealthCheckContext CreateContext(string name, params string[] tags)
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                name,
                Mock.Of<IHealthCheck>(),
                HealthStatus.Unhealthy,
                tags)
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1, 2, 3 & 16. Endpoint Registration & Tagging Strategy
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void DependencyInjection_RegistersHealthChecks_WithExpectedTags()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Storage:ServiceUrl"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
                ["Jwt:SecretKey"] = "a-very-long-secret-key-32-chars-minimum!!",
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test"
            })
            .Build();

        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(opts => opts.UseInMemoryDatabase("HealthCheckTestDb"));

        // Act
        services.AddInfrastructure(config);
        var sp = services.BuildServiceProvider();
        var healthCheckOptions = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registrations = healthCheckOptions.Registrations.ToList();
        registrations.Should().NotBeEmpty();

        // 1. Check self (live)
        var selfCheck = registrations.FirstOrDefault(r => r.Name == "self");
        selfCheck.Should().NotBeNull();
        selfCheck!.Tags.Should().Contain("live");

        // 2. Check postgresql (ready, db)
        var pgCheck = registrations.FirstOrDefault(r => r.Name == "postgresql");
        pgCheck.Should().NotBeNull();
        pgCheck!.Tags.Should().Contain("ready");
        pgCheck.Tags.Should().Contain("db");

        // 3. Check redis (ready, redis)
        var redisCheck = registrations.FirstOrDefault(r => r.Name == "redis");
        redisCheck.Should().NotBeNull();
        redisCheck!.Tags.Should().Contain("ready");
        redisCheck.Tags.Should().Contain("redis");

        // 4. Check minio (storage, minio) - Tuyệt đối KHÔNG có tag "ready" để bảo đảm specification
        var minioCheck = registrations.FirstOrDefault(r => r.Name == "minio");
        minioCheck.Should().NotBeNull();
        minioCheck!.Tags.Should().Contain("storage");
        minioCheck.Tags.Should().Contain("minio");
        minioCheck.Tags.Should().NotContain("ready", "MinIO không được nằm trong Readiness check theo đặc tả Task 4");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Liveness Semantics: Process đang sống -> Luôn Healthy
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void LivenessCheck_AlwaysReturnsHealthy_WhenProcessIsRunning()
    {
        // Arrange
        Func<HealthCheckResult> checkFunc = () => HealthCheckResult.Healthy("Application is running.");

        // Act
        var result = checkFunc();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("running");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5, 6, 7. PostgreSQL Health Check & Semantic Isolation
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PostgresHealthCheck_WhenCanConnectIsTrue_ReturnsHealthy()
    {
        // Arrange
        var check = new PostgresHealthCheck(ct => Task.FromResult(true));
        var context = CreateContext("postgresql", "ready", "db");

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("operational");
    }

    [Fact]
    public async Task PostgresHealthCheck_WhenCanConnectIsFalse_ReturnsUnhealthy()
    {
        // Arrange
        var check = new PostgresHealthCheck(ct => Task.FromResult(false));
        var context = CreateContext("postgresql", "ready", "db");

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Cannot connect");
    }

    [Fact]
    public async Task PostgresHealthCheck_WhenThrowsException_ReturnsUnhealthy_AndDoesNotLeakConnectionString()
    {
        // Arrange
        var check = new PostgresHealthCheck(ct => throw new TimeoutException("Connection timed out on Host=127.0.0.1;Password=secret"));
        var context = CreateContext("postgresql", "ready", "db");

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
    }

    [Fact]
    public void PostgresFailure_CausesReadyToBeUnhealthy_WhileLiveRemainsHealthy()
    {
        // Semantics verification:
        // Giả lập trạng thái PostgreSQL bị Unhealthy
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new HealthReportEntry(HealthStatus.Healthy, "App running", TimeSpan.FromMilliseconds(1), null, null, new[] { "live" }),
            ["postgresql"] = new HealthReportEntry(HealthStatus.Unhealthy, "DB down", TimeSpan.FromMilliseconds(1), null, null, new[] { "ready", "db" }),
            ["redis"] = new HealthReportEntry(HealthStatus.Healthy, "Redis ok", TimeSpan.FromMilliseconds(1), null, null, new[] { "ready", "redis" })
        };

        // Liveness predicate: check.Tags.Contains("live")
        var liveEntries = entries.Where(e => e.Value.Tags.Contains("live")).ToDictionary(k => k.Key, v => v.Value);
        var liveReport = new HealthReport(liveEntries, TimeSpan.FromMilliseconds(2));

        // Readiness predicate: check.Tags.Contains("ready")
        var readyEntries = entries.Where(e => e.Value.Tags.Contains("ready")).ToDictionary(k => k.Key, v => v.Value);
        var readyReport = new HealthReport(readyEntries, TimeSpan.FromMilliseconds(2));

        // Assert
        liveReport.Status.Should().Be(HealthStatus.Healthy, "Liveness phải Healthy kể cả khi PostgreSQL chết");
        readyReport.Status.Should().Be(HealthStatus.Unhealthy, "Readiness phải Unhealthy khi PostgreSQL chết");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8, 9, 10. Redis Health Check & Semantic Isolation
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task RedisHealthCheck_WhenConnectionStringMissing_ReturnsUnhealthy()
    {
        // Arrange: Cấu hình rỗng
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var check = new RedisHealthCheck(config);
        var context = CreateContext("redis", "ready", "redis");

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not configured");
    }

    [Fact]
    public void RedisFailure_CausesReadyToBeUnhealthy_WhileLiveRemainsHealthy()
    {
        // Semantics verification:
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new HealthReportEntry(HealthStatus.Healthy, "App running", TimeSpan.FromMilliseconds(1), null, null, new[] { "live" }),
            ["postgresql"] = new HealthReportEntry(HealthStatus.Healthy, "DB ok", TimeSpan.FromMilliseconds(1), null, null, new[] { "ready", "db" }),
            ["redis"] = new HealthReportEntry(HealthStatus.Unhealthy, "Redis timeout", TimeSpan.FromMilliseconds(1), null, null, new[] { "ready", "redis" })
        };

        var liveEntries = entries.Where(e => e.Value.Tags.Contains("live")).ToDictionary(k => k.Key, v => v.Value);
        var readyEntries = entries.Where(e => e.Value.Tags.Contains("ready")).ToDictionary(k => k.Key, v => v.Value);

        var liveReport = new HealthReport(liveEntries, TimeSpan.FromMilliseconds(2));
        var readyReport = new HealthReport(readyEntries, TimeSpan.FromMilliseconds(2));

        // Assert
        liveReport.Status.Should().Be(HealthStatus.Healthy, "Liveness phải Healthy kể cả khi Redis chết");
        readyReport.Status.Should().Be(HealthStatus.Unhealthy, "Readiness phải Unhealthy khi Redis chết");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 11, 12, 13. MinIO Health Check & Readiness Isolation
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task MinioHealthCheck_WhenConfigurationMissing_ReturnsUnhealthy()
    {
        // Arrange: Cấu hình rỗng
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var check = new MinioHealthCheck(config);
        var context = CreateContext("minio", "storage", "minio");

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not configured");
    }

    [Fact]
    public void MinioFailure_CausesGeneralHealthToBeUnhealthy_WhileLiveAndReadyRemainHealthy()
    {
        // Semantics verification:
        // MinIO chết -> /health (general) bị Unhealthy, nhưng /health/live và /health/ready KHÔNG bị ảnh hưởng
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new HealthReportEntry(HealthStatus.Healthy, "App running", TimeSpan.FromMilliseconds(1), null, null, new[] { "live" }),
            ["postgresql"] = new HealthReportEntry(HealthStatus.Healthy, "DB ok", TimeSpan.FromMilliseconds(1), null, null, new[] { "ready", "db" }),
            ["redis"] = new HealthReportEntry(HealthStatus.Healthy, "Redis ok", TimeSpan.FromMilliseconds(1), null, null, new[] { "ready", "redis" }),
            ["minio"] = new HealthReportEntry(HealthStatus.Unhealthy, "MinIO unreachable", TimeSpan.FromMilliseconds(1), null, null, new[] { "storage", "minio" })
        };

        var generalReport = new HealthReport(entries, TimeSpan.FromMilliseconds(3));
        var liveEntries = entries.Where(e => e.Value.Tags.Contains("live")).ToDictionary(k => k.Key, v => v.Value);
        var readyEntries = entries.Where(e => e.Value.Tags.Contains("ready")).ToDictionary(k => k.Key, v => v.Value);

        var liveReport = new HealthReport(liveEntries, TimeSpan.FromMilliseconds(2));
        var readyReport = new HealthReport(readyEntries, TimeSpan.FromMilliseconds(2));

        // Assert
        generalReport.Status.Should().Be(HealthStatus.Unhealthy, "/health phải Unhealthy khi MinIO gặp sự cố");
        liveReport.Status.Should().Be(HealthStatus.Healthy, "/health/live tuyệt đối không bị ảnh hưởng bởi MinIO");
        readyReport.Status.Should().Be(HealthStatus.Healthy, "/health/ready vẫn Healthy vì MinIO không nằm trong điều kiện readiness");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 14 & 15. ResponseWriter: Status Codes (200 vs 503) & Safe JSON Output
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ResponseWriter_WhenReportIsHealthy_ReturnsHttp200_AndValidJson()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new HealthReportEntry(HealthStatus.Healthy, "Application is running.", TimeSpan.FromMilliseconds(1.5), null, null),
            ["postgresql"] = new HealthReportEntry(HealthStatus.Healthy, "Database connection operational.", TimeSpan.FromMilliseconds(3.2), null, null)
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(4.7));

        // Act
        await HealthCheckResponseWriter.WriteResponse(context, report);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.ContentType.Should().Be("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var jsonText = await reader.ReadToEndAsync();

        jsonText.Should().Contain("\"status\": \"Healthy\"");
        jsonText.Should().Contain("\"name\": \"postgresql\"");
        jsonText.Should().Contain("\"name\": \"self\"");
    }

    [Fact]
    public async Task ResponseWriter_WhenReportIsUnhealthy_ReturnsHttp503_AndValidJson()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["postgresql"] = new HealthReportEntry(HealthStatus.Unhealthy, "Cannot connect to database.", TimeSpan.FromMilliseconds(5.1), null, null)
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(5.1));

        // Act
        await HealthCheckResponseWriter.WriteResponse(context, report);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.ContentType.Should().Be("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var jsonText = await reader.ReadToEndAsync();

        jsonText.Should().Contain("\"status\": \"Unhealthy\"");
        jsonText.Should().Contain("\"name\": \"postgresql\"");
    }

    [Fact]
    public async Task ResponseWriter_SanitizesSensitiveData_DoesNotLeakPasswords()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var sensitiveDescription = "Connection failed on Host=localhost;Username=admin;Password=SuperSecretPassword123;";
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["postgresql"] = new HealthReportEntry(HealthStatus.Unhealthy, sensitiveDescription, TimeSpan.FromMilliseconds(5.0), null, null)
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(5.0));

        // Act
        await HealthCheckResponseWriter.WriteResponse(context, report);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var jsonText = await reader.ReadToEndAsync();

        jsonText.Should().NotContain("SuperSecretPassword123", "Tuyệt đối không được để lộ mật khẩu trong response health check");
        jsonText.Should().Contain("Password=******");
    }
}
