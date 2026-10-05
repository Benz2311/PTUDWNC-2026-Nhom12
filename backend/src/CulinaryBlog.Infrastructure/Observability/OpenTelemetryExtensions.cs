using CulinaryBlog.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CulinaryBlog.Infrastructure.Observability;

/// <summary>
/// OpenTelemetry setup:
/// - OTLP exporter (gRPC)
/// - ASP.NET Core instrumentation (Trace API & HTTP Metrics)
/// - HttpClient instrumentation (Trace HTTP Client)
/// - EF Core instrumentation (Trace Database Queries)
/// - Custom ActivitySource & Meter: CulinaryBlog.Api
/// - Business Metrics: recipes.created, recipes.published
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? CulinaryBlogTelemetry.ServiceName;

        // Đăng ký telemetry singleton service
        services.AddSingleton<ICulinaryBlogTelemetry, CulinaryBlogTelemetry>();

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(CulinaryBlogTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()          // Trace API
                    .AddHttpClientInstrumentation()          // Trace HTTP
                    .AddEntityFrameworkCoreInstrumentation() // Trace DB
                    .AddOtlpExporter(opt =>
                    {
                        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var uri))
                        {
                            opt.Endpoint = uri;
                            opt.Protocol = OtlpExportProtocol.Grpc;
                        }
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(CulinaryBlogTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(opt =>
                    {
                        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var uri))
                        {
                            opt.Endpoint = uri;
                            opt.Protocol = OtlpExportProtocol.Grpc;
                        }
                    });
            });

        return services;
    }
}
