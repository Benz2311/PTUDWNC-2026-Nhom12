using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RecipeSlugHistory> RecipeSlugHistories => Set<RecipeSlugHistory>();
    public DbSet<RecipeAuditLog> RecipeAuditLogs => Set<RecipeAuditLog>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateConcurrencyTokens();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateConcurrencyTokens();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Category category:
                    category.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case Recipe recipe:
                    recipe.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case RecipeIngredient ingredient:
                    ingredient.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case RecipeStep step:
                    step.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case RecipeImage image:
                    image.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case RefreshToken refreshToken:
                    refreshToken.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property<NpgsqlTsVector>("SearchVectorFts")
                .HasColumnName("SearchVector")
                .HasColumnType("tsvector");

            entity.HasIndex("SearchVectorFts")
                .HasMethod("GIN");
        });

        modelBuilder.Entity<RecipeSlugHistory>(entity =>
        {
            entity.ToTable("RecipeSlugHistories");
            entity.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(220);
            entity.HasIndex(x => new { x.RecipeId, x.Slug })
                .IsUnique();
            entity.HasIndex(x => x.Slug);
            entity.HasOne<Recipe>()
                .WithMany()
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeAuditLog>(entity =>
        {
            entity.ToTable("RecipeAuditLogs");
            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(40);
            entity.HasIndex(x => new { x.RecipeId, x.CreatedAt });
        });
    }
}
