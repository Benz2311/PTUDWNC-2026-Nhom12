namespace CulinaryBlog.Application.Interfaces;

public interface IRecipeCacheInvalidator
{
    Task InvalidateAsync(Guid recipeId, string? slug, Guid categoryId, string? categorySlug, CancellationToken cancellationToken = default);
}
