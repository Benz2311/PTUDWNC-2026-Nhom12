using CulinaryBlog.API.Extensions;
using System.Diagnostics;
using System.Security.Claims;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Api.Middleware;
using CulinaryBlog.API.Middleware;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using CulinaryBlog.Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// ── Serilog — cấu hình trước khi build host ──────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting CulinaryBlog API");

    var builder = WebApplication.CreateBuilder(args);

    // Dùng Serilog thay thế logging mặc định
    builder.Host.UseSerilog();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddHostedService<CulinaryBlog.Api.Jobs.RecipePurgeHostedService>();
    builder.Services.AddHostedService<CulinaryBlog.Api.Jobs.SitemapGenerationHostedService>();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer(new CulinaryBlog.Api.OpenApi.BearerSecuritySchemeTransformer());
    });

    builder.Services.AddRateLimitingConfiguration(builder.Configuration);

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("RequireUser", policy => policy.RequireAuthenticatedUser())
        .AddPolicy("RequireAuthor", policy => policy.RequireRole("Author"))
        .AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"))
        .AddPolicy("RequireAuthorOrAdmin", policy => policy.RequireRole("Author", "Admin"));

    builder.Services.AddCors(options =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        if (builder.Environment.IsDevelopment())
        {
            var devOrigins = new[] { "http://localhost:3000", "http://localhost:5000", "http://127.0.0.1:3000" };
            allowedOrigins = allowedOrigins.Union(devOrigins, StringComparer.OrdinalIgnoreCase).ToArray();
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
                policy.SetIsOriginAllowed(_ => false)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });

    var app = builder.Build();

    // ── Migrate + Seed ────────────────────────────────────────────────────────
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Database migration skipped or tables already exist.");
            }

            try
            {
                await CategoryDataSeeder.SeedAsync(dbContext, targetCount: 20);
            }
            catch (Exception seedEx)
            {
                Log.Warning(seedEx, "Seeding skipped: {Message}", seedEx.Message);
            }

            try
            {
                var passwordHasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<ApplicationUser>>();
                if (!await dbContext.Users.AnyAsync(u => u.Email == "admin@culinaryblog.com"))
                {
                    var admin = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = "admin",
                        Email = "admin@culinaryblog.com",
                        NormalizedUserName = "ADMIN",
                        NormalizedEmail = "ADMIN@CULINARYBLOG.COM",
                        DisplayName = "Quản trị viên",
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        Roles = ["Admin", "Author"]
                    };
                    admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123");
                    await dbContext.Users.AddAsync(admin);
                    await dbContext.SaveChangesAsync();
                    Log.Information("Successfully seeded Admin user admin@culinaryblog.com");
                }

                if (!await dbContext.Users.AnyAsync(u => u.Email == "author@culinaryblog.com"))
                {
                    var author = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = "author",
                        Email = "author@culinaryblog.com",
                        NormalizedUserName = "AUTHOR",
                        NormalizedEmail = "AUTHOR@CULINARYBLOG.COM",
                        DisplayName = "Tác giả Demo",
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        Roles = ["Author"]
                    };
                    author.PasswordHash = passwordHasher.HashPassword(author, "Author@123");
                    await dbContext.Users.AddAsync(author);
                    await dbContext.SaveChangesAsync();
                    Log.Information("Successfully seeded Author user author@culinaryblog.com");
                }
            }
            catch (Exception initEx)
            {
                Log.Warning(initEx, "User seed skipped: {Message}", initEx.Message);
            }
        }
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────
    // 1. CorrelationId Middleware đặt đầu tiên để sinh/đọc header và gắn vào LogContext cho toàn bộ request flow
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseExceptionHandling();
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseCors("Frontend");

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    // Hangfire Dashboard (chỉ môi trường dev)
    if (app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire");
    }

    // 2. Serilog request logging — Structured Logging với đầy đủ properties & cảnh báo Slow Request > 500ms
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null || httpContext.Response.StatusCode >= 500)
            {
                return LogEventLevel.Error;
            }

            if (httpContext.Response.StatusCode >= 400)
            {
                return LogEventLevel.Warning;
            }

            if (elapsed > 500)
            {
                return LogEventLevel.Warning;
            }

            return LogEventLevel.Information;
        };

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? "/");
            diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
            diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);

            var correlationId = httpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                ?? CorrelationIdMiddleware.ResolveCorrelationId(httpContext);
            diagnosticContext.Set("CorrelationId", correlationId);

            var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User?.FindFirstValue("sub");
            if (!string.IsNullOrWhiteSpace(userId))
            {
                diagnosticContext.Set("UserId", userId);
            }

            if (Activity.Current != null)
            {
                diagnosticContext.Set("TraceId", Activity.Current.TraceId.ToString());
                diagnosticContext.Set("SpanId", Activity.Current.SpanId.ToString());
            }
        };
    });

    // 3. Custom telemetry metrics recorder middleware
    app.Use(async (context, next) =>
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var telemetry = context.RequestServices.GetService<ICulinaryBlogTelemetry>();
            telemetry?.RecordHttpRequest(
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    });

    app.MapCategoryEndpoints();
    app.MapRecipeEndpoints();
    CulinaryBlog.Api.Endpoints.Sitemap.SitemapEndpoints.MapSitemapEndpoints(app);
    app.MapControllers();

    // ── Health Check Endpoints (Task 4 - Võ Hùng Mạnh) ─────────────────────────
    // 1. /health - Tổng quan toàn bộ dependency (PostgreSQL, Redis, MinIO)
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = CulinaryBlog.Infrastructure.HealthChecks.HealthCheckResponseWriter.WriteResponse
    });

    // 2. /health/live - Liveness: Chỉ xác nhận process/app đang sống, không phụ thuộc external dependencies
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = CulinaryBlog.Infrastructure.HealthChecks.HealthCheckResponseWriter.WriteResponse
    });

    // 3. /health/ready - Readiness: Kiểm tra dependency bắt buộc tiếp nhận traffic (PostgreSQL + Redis)
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = CulinaryBlog.Infrastructure.HealthChecks.HealthCheckResponseWriter.WriteResponse
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}

namespace CulinaryBlog.API
{
    public class ApiEntryPoint
    {
    }
}
