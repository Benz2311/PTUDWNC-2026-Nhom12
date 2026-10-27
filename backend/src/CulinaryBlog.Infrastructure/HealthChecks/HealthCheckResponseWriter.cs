using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Infrastructure.HealthChecks;

/// <summary>
/// Định dạng response JSON an toàn, chuyên nghiệp cho Health Check endpoints.
/// Tuyệt đối không leak connection string, password, access/secret keys hay sensitive stack trace.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;

        // HTTP 200 cho Healthy và Degraded; HTTP 503 cho Unhealthy
        context.Response.StatusCode = report.Status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            TotalDuration = $"{report.TotalDuration.TotalMilliseconds:F1}ms",
            Timestamp = DateTime.UtcNow,
            Checks = report.Entries.Select(entry => new HealthCheckEntryDto
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = SanitizeDescription(entry.Value.Description),
                Duration = $"{entry.Value.Duration.TotalMilliseconds:F1}ms"
            }).ToList()
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, response, SerializerOptions);
    }

    /// <summary>
    /// Lọc bỏ các thông tin nhạy cảm nếu có xuất hiện trong thông điệp lỗi.
    /// </summary>
    public static string? SanitizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        var sanitized = description;
        if (sanitized.Contains("Password=", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                @"Password=[^;]+",
                "Password=******",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sanitized;
    }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public string TotalDuration { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<HealthCheckEntryDto> Checks { get; set; } = new();
}

public class HealthCheckEntryDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Duration { get; set; } = string.Empty;
}
