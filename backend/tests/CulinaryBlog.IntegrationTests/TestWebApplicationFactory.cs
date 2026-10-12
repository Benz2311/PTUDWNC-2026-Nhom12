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

namespace CulinaryBlog.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<CulinaryBlog.Api.Program>
{
    public TestWebApplicationFactory()
    {
        // Set environment variable BEFORE Program.Main() creates its builder
        // Use "Testing" so AddInfrastructure skips Hangfire and PostgreSQL
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(GetProjectPath("CulinaryBlog.Api"));

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            config.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables();
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Remove DatabaseInitializerHostedService to prevent it from running in tests
            var hostedServiceDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(DatabaseInitializerHostedService));
            if (hostedServiceDescriptor != null)
            {
                services.Remove(hostedServiceDescriptor);
            }

            // Add MemoryCache for RateLimitingMiddleware
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