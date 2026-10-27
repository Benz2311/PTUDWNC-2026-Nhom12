using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CulinaryBlog.Infrastructure.Persistence.Seed;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public class DatabaseInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;
    private readonly IHostEnvironment _environment;

    public DatabaseInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializerHostedService> logger,
        IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Skip in Testing environment (integration tests use in-memory DB)
        if (_environment.IsEnvironment("Testing"))
        {
            _logger.LogInformation("Skipping database initialization in Testing environment.");
            return;
        }

        _logger.LogInformation("Starting database initialization...");

        try
        {
            await DbInitializer.InitializeAsync(_serviceProvider, _logger, cancellationToken);
            _logger.LogInformation("Database initialization completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}