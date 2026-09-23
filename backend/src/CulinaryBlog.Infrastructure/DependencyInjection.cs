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
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // ── Authentication — ASP.NET Core Identity + JWT ─────────────────────
        var jwtSettings = configuration
            .GetSection(JwtSettings.Section)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt settings not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = jwtSettings.Issuer,
                    ValidateAudience         = true,
                    ValidAudience            = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        services.AddSingleton<JwtService>();

        // ── Redis Cache — StackExchange.Redis ─────────────────────────────────
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // ── Object Storage — AWSSDK.S3 → MinIO ───────────────────────────────
        services.AddSingleton<IStorageService, S3StorageService>();

        // ── Background Jobs — Hangfire + PostgreSQL ───────────────────────────
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

        // ── Email — MailKit ───────────────────────────────────────────────────
        services.AddSingleton<IEmailService, MailKitEmailService>();

        // ── Observability — OpenTelemetry ─────────────────────────────────────
        services.AddObservability(configuration);

        return services;
    }
}