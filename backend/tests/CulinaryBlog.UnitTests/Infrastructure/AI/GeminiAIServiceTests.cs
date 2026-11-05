using System.Net;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Infrastructure.AI;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CulinaryBlog.UnitTests.Infrastructure.AI;

public class GeminiAIServiceTests
{
    [Fact]
    public async Task GenerateTextAsync_SendsServerKeyAndReturnsGeneratedText()
    {
        var handler = new StubHandler((request, _) =>
        {
            request.Headers.GetValues("x-goog-api-key").Should().Contain("test-api-key");
            return Task.FromResult(JsonResponse("""{"candidates":[{"content":{"parts":[{"text":"{\"recipeIds\":[]}"}]}}]}"""));
        });
        var service = CreateService("test-api-key", handler);

        var result = await service.GenerateTextAsync("safe prompt", jsonResponse: true, CancellationToken.None);

        result.Should().Be("{\"recipeIds\":[]}");
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateFromImageAsync_SendsValidatedImageAsInlineData()
    {
        string? requestBody = null;
        var handler = new StubHandler(async (request, cancellationToken) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse("""{"candidates":[{"content":{"parts":[{"text":"{}"}]}}]}""");
        });
        var service = CreateService("test-api-key", handler);

        await service.GenerateFromImageAsync(
            "extract",
            new byte[] { 1, 2, 3 },
            "image/png",
            jsonResponse: true,
            CancellationToken.None);

        using var document = JsonDocument.Parse(requestBody!);
        var imagePart = document.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[1]
            .GetProperty("inline_data");
        imagePart.GetProperty("mime_type").GetString().Should().Be("image/png");
        imagePart.GetProperty("data").GetString().Should().Be(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task GenerateTextAsync_WithoutConfiguredKeyFailsBeforeSendingRequest()
    {
        var handler = new StubHandler((_, _) => throw new InvalidOperationException("Request should not be sent."));
        var service = CreateService(string.Empty, handler);

        var act = () => service.GenerateTextAsync("prompt", jsonResponse: false, CancellationToken.None);

        await act.Should().ThrowAsync<AIServiceException>()
            .Where(exception => exception.Code == "AI_PROVIDER_NOT_CONFIGURED");
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task GenerateTextAsync_WhenProviderFailsDoesNotExposeResponseBody()
    {
        var handler = new StubHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("sensitive provider response")
            }));
        var service = CreateService("test-api-key", handler);

        var act = () => service.GenerateTextAsync("prompt", jsonResponse: false, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AIServiceException>();
        exception.Which.Message.Should().NotContain("sensitive");
    }

    private static GeminiAIService CreateService(string apiKey, HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Gemini:ApiKey"] = apiKey,
                ["AI:Gemini:Model"] = "gemini-test",
                ["AI:Gemini:Endpoint"] = "https://example.test/v1beta"
            })
            .Build();
        return new GeminiAIService(
            new HttpClient(handler),
            configuration,
            Mock.Of<ILogger<GeminiAIService>>());
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return send(request, cancellationToken);
        }
    }
}
