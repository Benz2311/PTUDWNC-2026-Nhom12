using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;

namespace CulinaryBlog.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(GetProjectPath("CulinaryBlog.Api"));
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove any existing IHostedService for DbInitializer
            var hostedServices = services.Where(
                d => d.ServiceType == typeof(IHostedService) && 
                     d.ImplementationType?.Name.Contains("DbInitializer") == true).ToList();
            foreach (var hostedService in hostedServices)
            {
                services.Remove(hostedService);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure the host is built with the test configuration
        builder.ConfigureServices(services =>
        {
            // Remove any DbInitializer hosted service that might have been added
            var hostedServices = services.Where(
                d => d.ServiceType == typeof(IHostedService) && 
                     d.ImplementationType?.Name.Contains("DbInitializer") == true).ToList();
            foreach (var hostedService in hostedServices)
            {
                services.Remove(hostedService);
            }
        });
        
        var host = base.CreateHost(builder);
        
        // Seed the in-memory database with test data
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
            SeedTestData(context);
        }
        
        return host;
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        // Seed Categories
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                Category.Create("Món Việt", "mon-viet", "Các món ăn truyền thống Việt Nam", null, 1),
                Category.Create("Món Á", "mon-a", "Các món ăn châu Á", null, 2),
                Category.Create("Món Âu", "mon-au", "Các món ăn châu Âu", null, 3),
                Category.Create("Đồ Uống", "do-uong", "Các loại đồ uống giải khát, đồ uống có cồn", null, 4)
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();
        }
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