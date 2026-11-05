namespace CulinaryBlog.Application.Interfaces;

public interface IAIService
{
    Task<string> GenerateTextAsync(string prompt, bool jsonResponse, CancellationToken cancellationToken);
    Task<string> GenerateFromImageAsync(
        string prompt,
        ReadOnlyMemory<byte> image,
        string mimeType,
        bool jsonResponse,
        CancellationToken cancellationToken);
}
