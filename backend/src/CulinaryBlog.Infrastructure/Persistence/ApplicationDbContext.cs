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
    public DbSet<RecipeAuditLog> RecipeAuditLogs => Set<RecipeAuditLog>();
    public DbSet<RecipeSlugHistory> RecipeSlugHistories => Set<RecipeSlugHistory>();
    public DbSet<RecipeCollection> RecipeCollections => Set<RecipeCollection>();
    public DbSet<RecipeCollectionItem> RecipeCollectionItems => Set<RecipeCollectionItem>();
    public DbSet<RecipeView> RecipeViews => Set<RecipeView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (!isInMemory)
        {
            // Đăng ký PostgreSQL extensions phục vụ tìm kiếm tiếng Việt và fuzzy fallback theo SRS v1.2.0
            modelBuilder.HasPostgresExtension("unaccent");
            modelBuilder.HasPostgresExtension("pg_trgm");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.OwnsOne(r => r.Nutrition);

            // Cấu hình Shadow Property SearchVectorFts kiểu tsvector và ánh xạ vào cột "SearchVector"
            // Tránh xung đột với CLR property [NotMapped] string? SearchVector trên Recipe.cs của Domain
            if (!isInMemory)
            {
                entity.Property<NpgsqlTsVector>("SearchVectorFts")
                    .HasColumnName("SearchVector")
                    .HasColumnType("tsvector");
            }

            entity.Property(x => x.xmin).IsRowVersion();

            if (!isInMemory)
            {
                entity.HasIndex("SearchVectorFts")
                    .HasMethod("GIN");
            }
        });

        modelBuilder.Entity<RecipeSlugHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Slug).HasMaxLength(220).IsRequired();
            entity.HasIndex(x => x.Slug);
            entity.HasIndex(x => new { x.RecipeId, x.Slug }).IsUnique();
            entity.HasOne<Recipe>().WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeAuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.RecipeId, x.CreatedAt });
        });

        modelBuilder.Entity<RecipeCollection>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.IsDeleted });
            entity.HasOne(x => x.User)
                .WithMany(u => u.Collections)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeCollectionItem>(entity =>
        {
            entity.HasKey(x => new { x.CollectionId, x.RecipeId });
            entity.HasOne(x => x.Collection)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Recipe)
                .WithMany(r => r.CollectionItems)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeView>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IpAddress).HasMaxLength(45);
            entity.Property(x => x.UserAgent).HasMaxLength(255);
            entity.HasIndex(x => new { x.RecipeId, x.ViewedAt });
            entity.HasIndex(x => x.ViewedAt);
            entity.HasIndex(x => new { x.RecipeId, x.IpAddress, x.ViewedAt });
            entity.HasOne(x => x.Recipe)
                .WithMany(r => r.RecipeViews)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}