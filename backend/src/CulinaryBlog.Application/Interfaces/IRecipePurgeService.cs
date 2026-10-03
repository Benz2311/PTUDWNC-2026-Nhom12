namespace CulinaryBlog.Application.Interfaces;

public interface IRecipePurgeService
{
    Task<bool> PurgeAsync(Guid recipeId, Guid actorId, CancellationToken cancellationToken = default);
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}
