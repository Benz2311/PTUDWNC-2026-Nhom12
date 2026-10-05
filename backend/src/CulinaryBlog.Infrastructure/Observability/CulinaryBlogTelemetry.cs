using System.Diagnostics;
using System.Diagnostics.Metrics;
using CulinaryBlog.Application.Common.Interfaces;

namespace CulinaryBlog.Infrastructure.Observability;

/// <summary>
/// Quản lý tập trung toàn bộ OpenTelemetry ActivitySource, Meter và Metrics:
/// - ActivitySource: "CulinaryBlog.Api" (phục vụ Distributed Tracing)
/// - Meter: "CulinaryBlog.Api" (phục vụ Metrics)
/// - Counters: recipes.created, recipes.published, http.requests.total, http.requests.errors
/// - Histogram: http.request.duration.ms
/// </summary>
public sealed class CulinaryBlogTelemetry : ICulinaryBlogTelemetry, IDisposable
{
    public const string ServiceName = "CulinaryBlog.Api";
    public const string ActivitySourceName = "CulinaryBlog.Api";
    public const string MeterName = "CulinaryBlog.Api";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    private readonly Counter<long> _recipesCreatedCounter;
    private readonly Counter<long> _recipesPublishedCounter;
    private readonly Counter<long> _httpRequestsCounter;
    private readonly Counter<long> _httpErrorsCounter;
    private readonly Histogram<double> _httpRequestDurationHistogram;

    public CulinaryBlogTelemetry()
    {
        _recipesCreatedCounter = Meter.CreateCounter<long>(
            "recipes.created",
            unit: "{recipes}",
            description: "Number of recipes created");

        _recipesPublishedCounter = Meter.CreateCounter<long>(
            "recipes.published",
            unit: "{recipes}",
            description: "Number of recipes published");

        _httpRequestsCounter = Meter.CreateCounter<long>(
            "http.requests.total",
            unit: "{requests}",
            description: "Total HTTP requests handled");

        _httpErrorsCounter = Meter.CreateCounter<long>(
            "http.requests.errors",
            unit: "{errors}",
            description: "Total HTTP requests resulting in error (4xx or 5xx)");

        _httpRequestDurationHistogram = Meter.CreateHistogram<double>(
            "http.request.duration.ms",
            unit: "ms",
            description: "Duration of HTTP requests in milliseconds");
    }

    public void RecordRecipeCreated(string? category = null)
    {
        var tags = new TagList();
        if (!string.IsNullOrWhiteSpace(category))
        {
            tags.Add("category", category);
        }

        _recipesCreatedCounter.Add(1, tags);
    }

    public void RecordRecipePublished(string? category = null)
    {
        var tags = new TagList();
        if (!string.IsNullOrWhiteSpace(category))
        {
            tags.Add("category", category);
        }

        _recipesPublishedCounter.Add(1, tags);
    }

    public void RecordHttpRequest(string method, string path, int statusCode, double durationMs)
    {
        var tags = new TagList
        {
            { "http.method", method },
            { "http.route", path },
            { "http.status_code", statusCode },
        };

        _httpRequestsCounter.Add(1, tags);
        _httpRequestDurationHistogram.Record(durationMs, tags);

        if (statusCode >= 400)
        {
            _httpErrorsCounter.Add(1, tags);
        }
    }

    public void RecordError(string errorType, string? endpoint = null)
    {
        var tags = new TagList
        {
            { "error.type", errorType },
        };
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            tags.Add("endpoint", endpoint);
        }

        _httpErrorsCounter.Add(1, tags);
    }

    public void Dispose()
    {
        // ActivitySource và Meter là static/singleton
    }
}
