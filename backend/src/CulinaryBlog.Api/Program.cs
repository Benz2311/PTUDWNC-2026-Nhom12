using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using CulinaryBlog.Api.Middleware;
using CulinaryBlog.Api.OpenApi;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using Serilog;

try
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

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            policy
                .SetIsOriginAllowed(origin => 
                {
                    // Allow any localhost origin in development
                    if (builder.Environment.IsDevelopment())
                    {
                        return origin.StartsWith("http://localhost:") || 
                               origin.StartsWith("http://127.0.0.1:") ||
                               origin.StartsWith("https://localhost:") ||
                               origin.StartsWith("https://127.0.0.1:");
                    }
                    // In production, only allow specific origins
                    return origin == "http://localhost:5000" || 
                           origin == "http://localhost:3000";
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // ── Migrate + Seed ────────────────────────────────────────────────────────
    // Skip in Testing environment (integration tests use in-memory DB)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                await DbInitializer.InitializeAsync(services, logger);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialization failed.");
                throw;
            }
        }
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseExceptionHandling();

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

    // Serilog request logging
    app.UseSerilogRequestLogging();

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