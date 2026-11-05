using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Authentication;
using CulinaryBlog.Infrastructure.AI;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.Caching;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.HealthChecks;
using CulinaryBlog.Infrastructure.Observability;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Repositories;
using CulinaryBlog.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Minio;

namespace CulinaryBlog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.Section));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAuthenticationService, JwtTokenService>();
        services.AddScoped<CulinaryBlog.Application.Interfaces.IRecipeRepository, CulinaryBlog.Infrastructure.Persistence.RecipeRepository>();
        services.AddScoped<CulinaryBlog.Application.Contracts.Persistence.ICategoryRepository, CulinaryBlog.Infrastructure.Persistence.Repositories.CategoryRepository>();
        services.AddScoped<CulinaryBlog.Application.Contracts.Persistence.IRecipeRepository, CulinaryBlog.Infrastructure.Persistence.Repositories.RecipeRepository>();
        services.AddScoped<CulinaryBlog.Application.Repositories.IUserRepository, CulinaryBlog.Infrastructure.Repositories.UserRepository>();
        services.AddScoped<CulinaryBlog.Application.Repositories.IRefreshTokenRepository, CulinaryBlog.Infrastructure.Repositories.RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, ApplicationUnitOfWork>();
        services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<CulinaryBlog.Domain.Entities.ApplicationUser>, Microsoft.AspNetCore.Identity.PasswordHasher<CulinaryBlog.Domain.Entities.ApplicationUser>>();
        services.AddScoped<CulinaryBlog.Application.Interfaces.IJwtTokenGenerator, CulinaryBlog.Infrastructure.Authentication.JwtTokenGenerator>();
        services.AddScoped<CulinaryBlog.Application.Services.IAuthService, CulinaryBlog.Infrastructure.Services.AuthService>();
        services.AddScoped<IRecipeCacheInvalidator, RecipeCacheInvalidator>();
        services.AddScoped<IRecipePurgeService, RecipePurgeService>();
        services.AddScoped<ISitemapGenerator, SitemapGenerator>();
        services.AddHttpClient<IAIService, GeminiAIService>();

        services.AddSingleton<JwtService>();

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION"]
            ?? configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                try
                {
                    var redisOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnection);
                    redisOptions.ConnectTimeout = 1000;
                    redisOptions.SyncTimeout = 1000;
                    redisOptions.AsyncTimeout = 1000;
                    redisOptions.AbortOnConnectFail = false;
                    redisOptions.ConnectRetry = 1;
                    options.ConfigurationOptions = redisOptions;
                }
                catch
                {
                    options.Configuration = redisConnection;
                }

                options.InstanceName = "CulinaryBlog:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ResilientCacheService>();
        services.AddSingleton<CulinaryBlog.Application.Common.Interfaces.ICacheService>(provider =>
            provider.GetRequiredService<ResilientCacheService>());
        services.AddSingleton<CulinaryBlog.Application.Interfaces.ICacheService>(provider =>
            provider.GetRequiredService<ResilientCacheService>());

        var minioEndpoint = configuration["Minio:Endpoint"]
            ?? configuration["Storage:ServiceUrl"]
            ?? "http://localhost:9000";
        var minioUri = new Uri(minioEndpoint);
        var minioClient = new MinioClient()
            .WithEndpoint(minioUri.Host, minioUri.Port)
            .WithCredentials(
                configuration["Minio:AccessKey"] ?? configuration["Storage:AccessKey"] ?? throw new InvalidOperationException("Minio access key is not configured."),
                configuration["Minio:SecretKey"] ?? configuration["Storage:SecretKey"] ?? throw new InvalidOperationException("Minio secret key is not configured."))
            .WithSSL(minioUri.Scheme == Uri.UriSchemeHttps)
            .Build();
        services.AddSingleton<IMinioClient>(minioClient);
        services.AddSingleton<IStorageService, S3StorageService>();

        // ── Background Jobs — Hangfire + PostgreSQL ───────────────────────────
        var isTesting = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"],
            "Testing",
            StringComparison.OrdinalIgnoreCase);
        if (!isTesting)
        {
            services.AddHangfire(cfg => cfg
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    configuration.GetConnectionString("DefaultConnection"),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire"
                    }));

            services.AddHangfireServer();
            services.AddScoped<IBackgroundJobService, HangfireJobService>();
        }
        services.AddHttpClient();
        services.AddScoped<IImageResizeJob, ImageResizeJob>();

        // ── Email — MailKit ───────────────────────────────────────────────────
        services.AddSingleton<IEmailService, MailKitEmailService>();

        // ── Observability — OpenTelemetry ─────────────────────────────────────
        services.AddObservability(configuration);

        // ── Health Checks — ASP.NET Core Health Checks ────────────────────────
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running."), tags: new[] { "live" })
            .AddCheck<PostgresHealthCheck>("postgresql", tags: new[] { "ready", "db" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready", "redis" })
            .AddCheck<MinioHealthCheck>("minio", tags: new[] { "storage", "minio" });

        return services;
    }
}
