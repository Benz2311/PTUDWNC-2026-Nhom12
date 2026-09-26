using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

// ── Serilog — cấu hình trước khi build host ──────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
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
        await dbContext.Database.MigrateAsync();
        await CategoryDataSeeder.SeedAsync(dbContext, targetCount: 20);
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────
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

    app.MapCategoryEndpoints();
    app.MapRecipeEndpoints();

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