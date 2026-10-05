using System.Diagnostics;
using System.Security.Claims;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.API.Middleware;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
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

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            policy
                .WithOrigins("http://localhost:5000", "http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // ── Migrate + Seed ────────────────────────────────────────────────────────
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
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────
    // 1. CorrelationId Middleware đặt đầu tiên để sinh/đọc header và gắn vào LogContext cho toàn bộ request flow
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseCors("Frontend");

    app.UseAuthentication();
    app.UseAuthorization();

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
    app.MapControllers();

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
