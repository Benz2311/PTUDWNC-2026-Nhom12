using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.AI;

public sealed class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint;
    private readonly ILogger<GeminiAIService> _logger;

    public GeminiAIService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAIService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AI:Gemini:ApiKey"] ?? string.Empty;
        _model = configuration["AI:Gemini:Model"] ?? "gemini-2.5-flash";
        _endpoint = (configuration["AI:Gemini:Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta")
            .TrimEnd('/');
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public Task<string> GenerateTextAsync(string prompt, bool jsonResponse, CancellationToken cancellationToken) =>
        GenerateAsync(new
        {
            contents = new[] { new { parts = new object[] { new { text = prompt } } } },
            generationConfig = CreateGenerationConfig(jsonResponse)
        }, cancellationToken);

    public Task<string> GenerateFromImageAsync(
        string prompt,
        ReadOnlyMemory<byte> image,
        string mimeType,
        bool jsonResponse,
        CancellationToken cancellationToken) =>
        GenerateAsync(new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(image.Span) } }
                    }
                }
            },
            generationConfig = CreateGenerationConfig(jsonResponse)
        }, cancellationToken);

    private async Task<string> GenerateAsync(object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new AIServiceException("AI_PROVIDER_NOT_CONFIGURED", "Dịch vụ AI chưa được cấu hình.", false);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_endpoint}/models/{Uri.EscapeDataString(_model)}:generateContent")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-goog-api-key", _apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Gemini request failed with status {StatusCode} for model {Model}.",
                    (int)response.StatusCode,
                    _model);
                throw new AIServiceException("AI_PROVIDER_ERROR", "Nhà cung cấp AI hiện không xử lý được yêu cầu.", true);
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0 ||
                !candidates[0].TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array ||
                parts.GetArrayLength() == 0 ||
                !parts[0].TryGetProperty("text", out var responseText))
            {
                throw new AIServiceException("AI_EMPTY_RESPONSE", "Nhà cung cấp AI trả về kết quả trống.", true);
            }

            var text = responseText.GetString();
            if (string.IsNullOrWhiteSpace(text))
                throw new AIServiceException("AI_EMPTY_RESPONSE", "Nhà cung cấp AI trả về kết quả trống.", true);
            return text;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Gemini request failed for model {Model}.", _model);
            throw new AIServiceException("AI_PROVIDER_ERROR", "Không thể kết nối đến nhà cung cấp AI.", true);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Gemini request timed out for model {Model}.", _model);
            throw new AIServiceException("AI_PROVIDER_TIMEOUT", "Nhà cung cấp AI phản hồi quá chậm.", true);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Gemini returned malformed provider JSON for model {Model}.", _model);
            throw new AIServiceException("AI_INVALID_PROVIDER_RESPONSE", "Nhà cung cấp AI trả về phản hồi không hợp lệ.", false);
        }
    }

    private static object CreateGenerationConfig(bool jsonResponse) => jsonResponse
        ? new { responseMimeType = "application/json", temperature = 0.2 }
        : new { temperature = 0.3 };
}

public sealed class AIServiceException(string code, string message, bool retryable) : Exception(message)
{
    public string Code { get; } = code;
    public bool Retryable { get; } = retryable;
}
