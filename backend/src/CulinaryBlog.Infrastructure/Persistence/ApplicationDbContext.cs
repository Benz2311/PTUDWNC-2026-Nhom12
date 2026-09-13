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
    public DbSet<RecipeNutrition> RecipeNutritions => Set<RecipeNutrition>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.PasswordHash).HasMaxLength(500);

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
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);

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
            entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);

            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.AuthorId);
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.Status);

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

            entity.HasOne(x => x.Nutrition)
                .WithOne(x => x.Recipe)
                .HasForeignKey<RecipeNutrition>(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Quantity).HasMaxLength(100);
            entity.Property(x => x.Unit).HasMaxLength(50);
        });

        modelBuilder.Entity<RecipeStep>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Description).IsRequired();
        });

        modelBuilder.Entity<RecipeImage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Url).HasMaxLength(500).IsRequired();
            entity.Property(x => x.AltText).HasMaxLength(255);
        });

        modelBuilder.Entity<RecipeNutrition>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Calories).HasPrecision(10, 2);
            entity.Property(x => x.Protein).HasPrecision(10, 2);
            entity.Property(x => x.Carbohydrates).HasPrecision(10, 2);
            entity.Property(x => x.Fat).HasPrecision(10, 2);
            entity.Property(x => x.Fiber).HasPrecision(10, 2);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Token).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ReplacedByToken).HasMaxLength(500);
            entity.HasIndex(x => x.Token).IsUnique();
        });
    }
}
