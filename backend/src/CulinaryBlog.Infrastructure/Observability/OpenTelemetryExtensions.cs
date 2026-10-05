using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CulinaryBlog.Infrastructure.Observability;

/// <summary>
/// OpenTelemetry setup theo SRS:
/// - OTLP exporter
/// - ASP.NET Core instrumentation (Trace API)
/// - HttpClient instrumentation (Trace HTTP)
/// - EF Core instrumentation (Trace DB)
/// - Metrics
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";
        var serviceName   = configuration["OpenTelemetry:ServiceName"] ?? "CulinaryBlog.Api";

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()          // Trace API
                    .AddHttpClientInstrumentation()           // Trace HTTP
                    .AddEntityFrameworkCoreInstrumentation() // Trace DB
                    .AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(otlpEndpoint);
                        opt.Protocol = OtlpExportProtocol.Grpc;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(otlpEndpoint);
                        opt.Protocol = OtlpExportProtocol.Grpc;
                    });
            });

        return services;
    }
}
