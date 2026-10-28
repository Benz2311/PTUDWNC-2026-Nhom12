using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace CulinaryBlog.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeStep> RecipeSteps { get; }
    DbSet<RecipeImage> RecipeImages { get; }
    DbSet<RecipeIngredient> RecipeIngredients { get; }
    DbSet<Category> Categories { get; }
    DbSet<ApplicationUser> Users { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
