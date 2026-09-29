using CulinaryBlog.Application.Features.Recipes.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace CulinaryBlog.Application.Common.Interfaces;

/// <summary>
/// Giao diện Unit of Work quản lý transaction và đồng bộ thay đổi (Lab 3)
/// </summary>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IRecipeRepository Recipes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
