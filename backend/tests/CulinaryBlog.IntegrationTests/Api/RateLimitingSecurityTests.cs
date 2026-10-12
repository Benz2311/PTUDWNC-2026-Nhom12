using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using CulinaryBlog.Application.DTOs.Auth;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

/// <summary>
/// Factory với rate limit thấp để kiểm thử 429 (NFR-SEC-003).
/// Giữ môi trường Testing để skip Hangfire/PostgreSQL nhưng hạ limit qua in-memory config (factory-local, không ảnh hưởng test khác).
/// </summary>
public class RateLimitTestWebApplicationFactory : WebApplicationFactory<CulinaryBlog.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(GetProjectPath("CulinaryBlog.Api"));

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            config.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables();
            // Factory-local override — chỉ áp dụng cho factory này
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Auth:PermitLimit"] = "3",
                ["RateLimiting:Refresh:PermitLimit"] = "2"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("RateLimitTestDb");
            });

            var hostedServiceDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(DatabaseInitializerHostedService));
            if (hostedServiceDescriptor != null)
            {
                services.Remove(hostedServiceDescriptor);
            }

            services.AddMemoryCache();
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }

    private static string GetProjectPath(string projectName)
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir == null)
        {
            throw new InvalidOperationException("Solution root not found");
        }

        var projectPath = Path.Combine(currentDir.FullName, "src", projectName);
        if (!Directory.Exists(projectPath))
        {
            throw new InvalidOperationException($"Project path not found: {projectPath}");
        }

        return projectPath;
    }
}

public class RateLimitingSecurityTests : IClassFixture<RateLimitTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingSecurityTests(RateLimitTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LoginEndpoint_ExceedsAuthLimit_Returns429WithProblemDetails()
    {
        // Arrange — auth limit is 3 requests/minute in this factory
        var request = new LoginRequest
        {
            UserNameOrEmail = "ratelimit_" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!"
        };

        // Act — send more requests than the limit (3 allowed, 4th should be 429)
        HttpResponseMessage lastResponse = null!;
        for (var i = 0; i < 5; i++)
        {
            lastResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", request);
        }

        // Assert
        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        lastResponse.Headers.RetryAfter.Should().NotBeNull();

        var problem = await lastResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(429);
        problem.GetProperty("title").GetString().Should().Be("Too Many Requests");
    }

    [Fact]
    public async Task RefreshEndpoint_ExceedsRefreshLimit_Returns429()
    {
        // Arrange — refresh limit is 2 requests/minute in this factory
        var request = new RefreshRequest { RefreshToken = "invalid_token" };

        // Act
        HttpResponseMessage lastResponse = null!;
        for (var i = 0; i < 5; i++)
        {
            lastResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", request);
        }

        // Assert
        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        lastResponse.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task GeneralApiEndpoint_UnderApiLimit_ReturnsNon429()
    {
        // Arrange — general API limit is high (10000 in Testing via env default when not overridden)
        // Act — categories is a public endpoint (if exists) or returns 401/404, but NOT 429
        var response = await _client.GetAsync("/api/v1/categories");

        // Assert — should not be rate limited (429) for a few requests
        response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }
}
