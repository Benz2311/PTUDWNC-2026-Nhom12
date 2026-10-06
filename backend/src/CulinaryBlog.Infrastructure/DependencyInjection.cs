using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CulinaryBlog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var isTesting = environment?.IsEnvironment("Testing") == true;

        // ── Database — PostgreSQL + EF Core ──────────────────────────────────
        if (isTesting)
        {
            // In tests, the DbContext is configured by the test factory (in-memory)
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });
        }

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // ── Identity ────────────────────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // ── JwtSettings from configuration ─────────────────────────────────────
        services.Configure<Application.Interfaces.JwtSettings>(configuration.GetSection(Application.Interfaces.JwtSettings.Section));

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<CulinaryBlog.Application.Contracts.Persistence.ICategoryRepository, CategoryRepository>();
        services.AddScoped<CulinaryBlog.Application.Contracts.Persistence.IRecipeRepository, CulinaryBlog.Infrastructure.Persistence.Repositories.RecipeRepository>();
        services.AddScoped<CulinaryBlog.Application.Interfaces.IRecipeRepository, CulinaryBlog.Infrastructure.Persistence.RecipeRepository>();
        services.AddScoped<IUnitOfWork, ApplicationUnitOfWork>();
        services.AddScoped<CulinaryBlog.Application.Repositories.IUserRepository, CulinaryBlog.Infrastructure.Repositories.UserRepository>();
        services.AddScoped<CulinaryBlog.Application.Repositories.IRefreshTokenRepository, CulinaryBlog.Infrastructure.Repositories.RefreshTokenRepository>();
        services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<ApplicationUser>, Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>>();

        // ── Auth Services ──────────────────────────────────────────────────────
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<CulinaryBlog.Application.Services.IAuthService, CulinaryBlog.Infrastructure.Services.AuthService>();

        // ── Authentication — ASP.NET Core Identity + JWT ─────────────────────
        var jwtSettings = configuration
            .GetSection(Application.Interfaces.JwtSettings.Section)
            .Get<Application.Interfaces.JwtSettings>()
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
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        services.AddSingleton<JwtService>();

        // ── Redis Cache — StackExchange.Redis / Resilient Cache ──────────────
        var redisConnection = configuration.GetConnectionString("Redis") ?? configuration["REDIS_CONNECTION"];
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
        services.AddSingleton<CulinaryBlog.Application.Common.Interfaces.ICacheService>(sp => sp.GetRequiredService<ResilientCacheService>());
        services.AddSingleton<CulinaryBlog.Application.Interfaces.ICacheService>(sp => sp.GetRequiredService<ResilientCacheService>());

        // ── Object Storage — AWSSDK.S3 → MinIO ───────────────────────────────
        services.AddSingleton<IStorageService, S3StorageService>();

        // ── Background Jobs — Hangfire + PostgreSQL ───────────────────────────
        if (!isTesting)
        {
            services.AddHangfire(cfg => cfg
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(configuration.GetConnectionString("DefaultConnection"), new PostgreSqlStorageOptions { SchemaName = "hangfire" }));

            services.AddHangfireServer();
            services.AddScoped<IBackgroundJobService, HangfireJobService>();
        }

        // ── Email — MailKit ───────────────────────────────────────────────────
        services.AddSingleton<IEmailService, MailKitEmailService>();

        // ── Observability — OpenTelemetry ─────────────────────────────────────
        services.AddObservability(configuration);

        return services;
    }
}