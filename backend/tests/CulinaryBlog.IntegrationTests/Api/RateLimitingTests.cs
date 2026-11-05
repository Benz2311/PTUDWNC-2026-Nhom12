using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using System.Threading.RateLimiting;
using CulinaryBlog.API.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

public sealed class RateLimitingTests
{
    [Fact]
    public void RateLimiting_RegistersThreeDistinctPolicies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddLogging();
        services.AddRateLimitingConfiguration(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var rateLimiterOptions = serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        RateLimitingExtensions.AuthPolicy.Should().Be("AuthPolicy");
        RateLimitingExtensions.GeneralPolicy.Should().Be("GeneralPolicy");
        RateLimitingExtensions.UploadPolicy.Should().Be("UploadPolicy");

        // Không duplicate policy
        var policies = new[] { RateLimitingExtensions.AuthPolicy, RateLimitingExtensions.GeneralPolicy, RateLimitingExtensions.UploadPolicy };
        policies.Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void RateLimiting_DefaultConfigurationLimits_MatchSrsRequirements()
    {
        var configuration = new ConfigurationBuilder().Build();

        var authLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var generalLimit = configuration.GetValue("RateLimiting:General:PermitLimit", 100);
        var uploadLimit = configuration.GetValue("RateLimiting:Upload:PermitLimit", 5);

        authLimit.Should().Be(10, "SRS yêu cầu Auth limit 10 req/min/IP");
        generalLimit.Should().Be(100, "SRS yêu cầu General limit 100 req/min");
        uploadLimit.Should().Be(5, "SRS yêu cầu Upload limit 5 req/min/IP");
    }

    [Fact]
    public async Task RateLimiting_GeneralPolicy_WhenExceeded_Returns429ProblemDetails()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["RateLimiting:General:PermitLimit"] = "2",
            ["RateLimiting:General:WindowSeconds"] = "60"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        builder.Services.AddRateLimitingConfiguration(configuration);

        var app = builder.Build();
        app.UseRateLimiter();

        app.MapGet("/api/v1/test-general", () => Results.Ok(new { message = "ok" }))
           .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        await app.StartAsync();
        var client = app.GetTestClient();

        var res1 = await client.GetAsync("/api/v1/test-general");
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        var res2 = await client.GetAsync("/api/v1/test-general");
        res2.StatusCode.Should().Be(HttpStatusCode.OK);

        var res3 = await client.GetAsync("/api/v1/test-general");
        res3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        res3.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var body = await res3.Content.ReadAsStringAsync();
        body.Should().Contain("RATE_LIMIT_EXCEEDED");
        body.Should().Contain("Too Many Requests");
    }

    [Fact]
    public async Task RateLimiting_GeneralPolicy_UsesIndependentLimitsPerAuthenticatedUser()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["RateLimiting:General:PermitLimit"] = "1",
            ["RateLimiting:General:WindowSeconds"] = "60"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        builder.Services.AddRateLimitingConfiguration(configuration);

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            var userId = context.Request.Headers["X-Test-User"].ToString();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                "test"));
            await next();
        });
        app.UseRateLimiter();
        app.MapGet("/api/v1/test-user", () => Results.Ok())
            .RequireRateLimiting(RateLimitingExtensions.GeneralPolicy);

        await app.StartAsync();
        var client = app.GetTestClient();

        using var requests = new SemaphoreSlim(20);
        var results = await Task.WhenAll(Enumerable.Range(1, 100).Select(async userNumber =>
        {
            await requests.WaitAsync();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test-user");
                request.Headers.Add("X-Test-User", $"user-{userNumber}");
                using var response = await client.SendAsync(request);
                return response.StatusCode;
            }
            finally
            {
                requests.Release();
            }
        }));

        results.Should().OnlyContain(status => status == HttpStatusCode.OK);

        using var repeatRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test-user");
        repeatRequest.Headers.Add("X-Test-User", "user-1");
        var repeatedResponse = await client.SendAsync(repeatRequest);
        repeatedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimiting_AuthPolicy_WhenExceeded_Returns429ProblemDetails()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["RateLimiting:Auth:PermitLimit"] = "2",
            ["RateLimiting:Auth:WindowSeconds"] = "60"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        builder.Services.AddRateLimitingConfiguration(configuration);

        var app = builder.Build();
        app.UseRateLimiter();

        app.MapPost("/api/v1/auth/test-login", () => Results.Ok(new { message = "logged in" }))
           .RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

        await app.StartAsync();
        var client = app.GetTestClient();

        var res1 = await client.PostAsync("/api/v1/auth/test-login", null);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        var res2 = await client.PostAsync("/api/v1/auth/test-login", null);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);

        var res3 = await client.PostAsync("/api/v1/auth/test-login", null);
        res3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        res3.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var body = await res3.Content.ReadAsStringAsync();
        body.Should().Contain("RATE_LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task RateLimiting_UploadPolicy_WhenExceeded_Returns429ProblemDetails()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["RateLimiting:Upload:PermitLimit"] = "2",
            ["RateLimiting:Upload:WindowSeconds"] = "60"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        builder.Services.AddRateLimitingConfiguration(configuration);

        var app = builder.Build();
        app.UseRateLimiter();

        app.MapPost("/api/v1/recipes/1/images", () => Results.Ok(new { message = "uploaded" }))
           .RequireRateLimiting(RateLimitingExtensions.UploadPolicy);

        await app.StartAsync();
        var client = app.GetTestClient();

        var res1 = await client.PostAsync("/api/v1/recipes/1/images", null);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        var res2 = await client.PostAsync("/api/v1/recipes/1/images", null);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);

        var res3 = await client.PostAsync("/api/v1/recipes/1/images", null);
        res3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        res3.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var body = await res3.Content.ReadAsStringAsync();
        body.Should().Contain("RATE_LIMIT_EXCEEDED");
    }
}
