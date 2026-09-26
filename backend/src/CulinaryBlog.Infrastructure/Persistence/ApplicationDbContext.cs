using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasColumnName("DisplayName").HasMaxLength(100).IsRequired();
            entity.Property(x => x.UserName).HasMaxLength(256);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.EmailConfirmed).IsRequired();
            entity.Property(x => x.Roles).HasColumnType("text[]").IsRequired();
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.PasswordHash).HasColumnType("text");

            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Recipes)
                .WithOne(x => x.Author)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasColumnType("text");
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.RowVersion).HasColumnName("RowVersion").IsConcurrencyToken().ValueGeneratedNever();

            entity.HasIndex(x => x.Slug).IsUnique();

            entity.HasMany(x => x.Recipes)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Content).HasColumnName("Instructions").IsRequired();
            entity.Property(x => x.Description).HasColumnType("text").IsRequired();
            entity.Property(x => x.PrepTimeMinutes).HasColumnName("PrepTimeMinutes").IsRequired();
            entity.Property(x => x.CookTimeMinutes).HasColumnName("CookTimeMinutes").IsRequired();
            entity.Property(x => x.Servings).IsRequired();
            entity.Property(x => x.RowVersion).HasColumnName("RowVersion").IsConcurrencyToken().ValueGeneratedNever();

            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.AuthorId);
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.IsDeleted, x.Status, x.CreatedAt });
            entity.HasIndex(x => new { x.CategoryId, x.IsDeleted, x.Status, x.CreatedAt });
            entity.HasIndex(x => x.Title)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
            entity.HasIndex(x => x.Description)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            entity.HasOne(x => x.Author)
                .WithMany(x => x.Recipes)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Recipes)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Ingredients)
                .WithOne(x => x.Recipe)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Steps)
                .WithOne(x => x.Recipe)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Images)
                .WithOne(x => x.Recipe)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.OwnsOne(x => x.Nutrition, nutrition =>
            {
                nutrition.Property(x => x.Calories).HasColumnName("Nutrition_Calories").HasPrecision(8, 2);
                nutrition.Property(x => x.Protein).HasColumnName("Nutrition_Protein").HasPrecision(8, 2);
                nutrition.Property(x => x.Carbohydrates).HasColumnName("Nutrition_Carbohydrates").HasPrecision(8, 2);
                nutrition.Property(x => x.Fat).HasColumnName("Nutrition_Fat").HasPrecision(8, 2);
                nutrition.Property(x => x.Fiber).HasColumnName("Nutrition_Fiber").HasPrecision(8, 2);
                nutrition.Property(x => x.Sodium).HasColumnName("Nutrition_Sodium").HasPrecision(8, 2);
            });
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(10, 3);
            entity.Property(x => x.Unit).HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.SortOrder).HasColumnName("OrderIndex");
            entity.Property(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            entity.HasIndex(x => x.RecipeId);
        });

        modelBuilder.Entity<RecipeStep>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            entity.HasIndex(x => new { x.RecipeId, x.StepNumber }).IsUnique();
        });

        modelBuilder.Entity<RecipeImage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Url).HasColumnName("OriginalUrl").HasMaxLength(500).IsRequired();
            entity.Property(x => x.AltText).HasMaxLength(200);
            entity.Property(x => x.SortOrder).HasColumnName("OrderIndex");
            entity.Property(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            entity.HasIndex(x => new { x.RecipeId, x.SortOrder });
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Token).HasColumnName("TokenHash").HasMaxLength(64).IsRequired();
            entity.Property(x => x.ReplacedByToken).HasColumnName("ReplacedByTokenHash").HasMaxLength(64);
            entity.Property(x => x.CreatedByIp).HasMaxLength(45);
            entity.Property(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            entity.HasIndex(x => x.Token).IsUnique();
        });
    }
}
