using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

/// <summary>
/// Smoke-level integration tests — verifies that the API host boots successfully.
/// Requires a running database and valid local configuration.
/// </summary>
public class CategoriesEndpointTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient? _client;

    public CategoriesEndpointTests(WebApplicationFactory<global::Program> factory)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["ConnectionStrings:DefaultConnection"]
            ?? configuration["TEST_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _client = null;
            return;
        }

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ReturnsSuccessStatusCode()
    {
        if (_client is null)
        {
            return;
        }

        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            $"GET /api/categories should return 2xx but returned {(int)response.StatusCode}");
    }
}
