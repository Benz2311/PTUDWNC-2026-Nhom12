namespace CulinaryBlog.Application.Interfaces;

public interface ISitemapGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
