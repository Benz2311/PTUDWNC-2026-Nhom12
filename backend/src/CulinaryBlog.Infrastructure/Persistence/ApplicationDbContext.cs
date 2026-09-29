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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();

    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Đăng ký PostgreSQL extensions phục vụ tìm kiếm tiếng Việt và fuzzy fallback theo SRS v1.2.0
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.OwnsOne(r => r.Nutrition);

            // Cấu hình Shadow Property SearchVectorFts kiểu tsvector và ánh xạ vào cột "SearchVector"
            // Tránh xung đột với CLR property [NotMapped] string? SearchVector trên Recipe.cs của Domain
            entity.Property<NpgsqlTsVector>("SearchVectorFts")
                .HasColumnName("SearchVector")
                .HasColumnType("tsvector");

            entity.HasIndex("SearchVectorFts")
                .HasMethod("GIN");
        });
    }
}