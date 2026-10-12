using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Api.Middleware;
using CulinaryBlog.Api.OpenApi;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using Serilog;

namespace CulinaryBlog.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Serilog — cấu hình sau khi có builder ──────────────────────────────────
        // Make Serilog config resilient for test environments
        try
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
        }
        catch
        {
            // Fallback to basic console logger if config fails (e.g., in tests)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }

        Log.Information("Starting CulinaryBlog API");

        // Dùng Serilog thay thế logging mặc định
        builder.Host.UseSerilog();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi(options =>
        {
            // Swagger/Scalar: hỗ trợ nhập JWT Bearer Token
            options.AddDocumentTransformer(new BearerSecuritySchemeTransformer());
        });

        // Authorization policies
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("RequireUser", policy => policy.RequireRole("User"))
            .AddPolicy("RequireAuthor", policy => policy.RequireRole("Author"))
            .AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"))
            .AddPolicy("RequireAuthorOrAdmin", policy => policy.RequireRole("Author", "Admin"));

        builder.Services.AddCors(options =>
        {
            // NFR-SEC-005: Only allow configured origins — never wildcard "*".
            // Development: http://localhost:3000 (SRS). Production: read from "Cors:AllowedOrigins".
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            if (builder.Environment.IsDevelopment() &&
                !allowedOrigins.Contains("http://localhost:3000", StringComparer.OrdinalIgnoreCase))
            {
                allowedOrigins = allowedOrigins.Append("http://localhost:3000").ToArray();
            }

            options.AddPolicy("Frontend", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    // Fail closed: reject every origin when none is configured.
                    policy.SetIsOriginAllowed(_ => false)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        // Rate limiting configuration - higher limits in Testing environment
        // Limits can be overridden via configuration (e.g. tests).
        // Defaults: Auth=10, Refresh=5, Api=100, Upload=5 requests/min/IP (NFR-SEC-003)
        var isTesting = builder.Environment.IsEnvironment("Testing");
        var defaultAuthLimit = isTesting ? 1000 : 10;
        var defaultRefreshLimit = isTesting ? 1000 : 5;
        var defaultApiLimit = isTesting ? 10000 : 100;
        var defaultUploadLimit = isTesting ? 1000 : 5;

        // Keep a reference to the configuration manager — it is finalized during Build()
        // (test-factory ConfigureAppConfiguration sources are applied at that point).
        var configuration = builder.Configuration;

        var app = builder.Build();

        // ── Middleware pipeline ───────────────────────────────────────────────────
        app.UseExceptionHandling();

        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseCors("Frontend");

        // Read rate limits AFTER Build() so test-factory configuration overrides
        // (ConfigureAppConfiguration) are fully applied before values are captured.
        var authRateLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", defaultAuthLimit);
        var refreshRateLimit = configuration.GetValue("RateLimiting:Refresh:PermitLimit", defaultRefreshLimit);
        var apiRateLimit = configuration.GetValue("RateLimiting:Api:PermitLimit", defaultApiLimit);
        var uploadRateLimit = configuration.GetValue("RateLimiting:Upload:PermitLimit", defaultUploadLimit);

        // Rate limiting: apply specific policies per SRS NFR-SEC-003
        // Auth endpoints: 10 request/min/IP
        app.UseRateLimitingPolicy("auth", authRateLimit, 1, new[] { "/api/v1/auth/login", "/api/v1/auth/register", "/api/v1/auth/google", "/api/v1/auth/email/resend" });
        // Refresh/logout endpoints: 5 request/min/IP
        app.UseRateLimitingPolicy("refresh", refreshRateLimit, 1, new[] { "/api/v1/auth/refresh", "/api/v1/auth/refresh-token", "/api/v1/auth/revoke", "/api/v1/auth/logout" });
        // Upload endpoints: 5 request/min/IP (POST only — file upload)
        app.UseRateLimitingPolicy("upload", uploadRateLimit, 1, new[] { "/api/v1/recipes/", "/api/v1/admin/recipes/" }, new[] { "POST" });
        // General API: 100 request/min/IP
        app.UseRateLimitingPolicy("api", apiRateLimit, 1, new[] { "/api/v1/" });

        app.UseAuthentication();
        app.UseAuthorization();

        // Hangfire Dashboard (chỉ môi trường dev)
        if (app.Environment.IsDevelopment())
        {
            app.UseHangfireDashboard("/hangfire");
        }

        // Serilog request logging
        app.UseSerilogRequestLogging();

        app.MapControllers();

        await app.RunAsync();
    }
}