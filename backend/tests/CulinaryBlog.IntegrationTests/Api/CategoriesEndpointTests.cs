using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

/// <summary>
/// Smoke-level integration tests — verifies that the API host boots successfully.
/// Requires a running database; skipped automatically if test connection is not configured.
/// </summary>
public class CategoriesEndpointTests
{
    [PostgreSqlFact]
    public async Task GetCategories_ReturnsSuccessStatusCode()
    {
        await using var factory = new WebApplicationFactory<CulinaryBlog.API.ApiEntryPoint>();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/categories");

        // Assert
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
                Skip = "Skipped: requires a running PostgreSQL database.";
            }
        }
    }
}
