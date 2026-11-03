using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

public sealed class CategoriesEndpointTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly WebApplicationFactory<global::Program> _factory;

    public CategoriesEndpointTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    [PostgreSqlFact]
    public async Task GetCategories_ReturnsSuccessStatusCode()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/categories");
        response.IsSuccessStatusCode.Should().BeTrue(
            $"GET /api/categories should return 2xx but returned {(int)response.StatusCode}");
    }

    private sealed class PostgreSqlFactAttribute : FactAttribute
    {
        public PostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CULINARYBLOG_TEST_CONNECTION")) &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")))
            {
                Skip = "Set CULINARYBLOG_TEST_CONNECTION or TEST_CONNECTION_STRING to a dedicated PostgreSQL database.";
            }
        }
    }
}
