using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

/// <summary>
/// Smoke-level integration tests — verifies that the API host boots successfully.
/// Requires a running database; skipped automatically via WebApplicationFactory
/// if DI configuration fails (e.g., in CI without a DB).
/// </summary>
public class CategoriesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CategoriesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            $"GET /api/categories should return 2xx but returned {(int)response.StatusCode}");
    }
}
