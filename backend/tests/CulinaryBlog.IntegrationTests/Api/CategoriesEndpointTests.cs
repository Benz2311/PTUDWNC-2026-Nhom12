using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

/// <summary>
/// Smoke-level integration tests — verifies that the API host boots successfully.
/// Uses in-memory database for testing.
/// </summary>
public class CategoriesEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    static CategoriesEndpointTests()
    {
        // Set environment variable before factory is created so Program.Main() picks it up
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    public CategoriesEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/categories");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            $"GET /api/v1/categories should return 2xx but returned {(int)response.StatusCode}");
    }
}
