using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Authentication;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.Caching;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.Observability;
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

namespace CulinaryBlog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database — PostgreSQL + EF Core ──────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var host = configuration["POSTGRES_HOST"] ?? "localhost";
            var defaultPort = "5432";
            try
            {
                using var tcp = new System.Net.Sockets.TcpClient("127.0.0.1", 5433);
                defaultPort = "5433";
            }
            catch
            {
                defaultPort = "5432";
            }

            var port = configuration["POSTGRES_PORT"] ?? defaultPort;
            var database = configuration["POSTGRES_DB"] ?? "culinary_blog";
            var username = configuration["POSTGRES_USER"] ?? "postgres";
            var password = configuration["POSTGRES_PASSWORD"] ?? string.Empty;

            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";
        }
        else if (Environment.GetEnvironmentVariable("POSTGRES_PORT") is null && connectionString.Contains("Port=5433"))
        {
            try
            {
                using var tcp = new System.Net.Sockets.TcpClient("127.0.0.1", 5433);
            }
            catch
            {
                connectionString = connectionString.Replace("Port=5433", "Port=5432");
            }
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // ── Repositories & Unit of Work (TV1, TV2, TV3, TV4) ────────────────
        services.AddScoped<CulinaryBlog.Application.Contracts.Persistence.ICategoryRepository, CategoryRepository>();
        services.AddScoped<CulinaryBlog.Application.Contracts.Persistence.IRecipeRepository, CulinaryBlog.Infrastructure.Persistence.Repositories.RecipeRepository>();
        services.AddScoped<CulinaryBlog.Application.Interfaces.IRecipeRepository, CulinaryBlog.Infrastructure.Persistence.RecipeRepository>();
        services.AddScoped<CulinaryBlog.Application.Interfaces.IUnitOfWork, ApplicationUnitOfWork>();

        // Lab 3 TV4 Generic Repository & Unit of Work
        services.AddScoped(typeof(CulinaryBlog.Application.Common.Interfaces.IRepository<>), typeof(CulinaryBlog.Infrastructure.Persistence.Repositories.Repository<>));
        services.AddScoped<CulinaryBlog.Application.Features.Recipes.Interfaces.IRecipeRepository, CulinaryBlog.Infrastructure.Persistence.Repositories.RecipeRepository>();
        services.AddScoped<CulinaryBlog.Application.Common.Interfaces.IUnitOfWork, CulinaryBlog.Infrastructure.Persistence.UnitOfWork.UnitOfWork>();

        // TV1 Repositories & Services
        services.AddScoped<CulinaryBlog.Application.Repositories.IUserRepository, CulinaryBlog.Infrastructure.Repositories.UserRepository>();
        services.AddScoped<CulinaryBlog.Application.Repositories.IRefreshTokenRepository, CulinaryBlog.Infrastructure.Repositories.RefreshTokenRepository>();
        services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<CulinaryBlog.Domain.Entities.ApplicationUser>, Microsoft.AspNetCore.Identity.PasswordHasher<CulinaryBlog.Domain.Entities.ApplicationUser>>();
        services.AddScoped<CulinaryBlog.Application.Services.IAuthService, CulinaryBlog.Infrastructure.Services.AuthService>();

        // ── Authentication — ASP.NET Core Identity + JWT (TV1) ───────────────
        var jwtSettings = configuration
            .GetSection(JwtSettings.Section)
            .Get<JwtSettings>();

        if (jwtSettings != null && !string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        }
        else
        {
            services.AddAuthentication();
        }

        services.AddAuthorization();
        services.AddSingleton<JwtService>();

        // ── Redis Cache — StackExchange.Redis / Resilient Cache (TV4) ────────
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "CulinaryBlog:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ResilientCacheService>();
        services.AddSingleton<CulinaryBlog.Application.Common.Interfaces.ICacheService>(sp =>
            sp.GetRequiredService<ResilientCacheService>());
        services.AddSingleton<CulinaryBlog.Application.Interfaces.ICacheService>(sp =>
            sp.GetRequiredService<ResilientCacheService>());

        // ── Object Storage — AWSSDK.S3 → MinIO ───────────────────────────────
        services.AddSingleton<IStorageService, S3StorageService>();

        // ── Background Jobs — Hangfire + PostgreSQL ───────────────────────────
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                services.AddHangfire(cfg => cfg
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(
                        connectionString,
                        new PostgreSqlStorageOptions
                        {
                            SchemaName = "hangfire"
                        }));

                services.AddHangfireServer();
            }
            catch
            {
                // Fail-safe nếu hangfire schema chưa sẵn sàng
            }
        }
        services.AddScoped<IBackgroundJobService, HangfireJobService>();

        // ── Email — MailKit ───────────────────────────────────────────────────
        services.AddSingleton<IEmailService, MailKitEmailService>();

        // ── Observability — OpenTelemetry ─────────────────────────────────────
        services.AddObservability(configuration);

        return services;
    }
}
