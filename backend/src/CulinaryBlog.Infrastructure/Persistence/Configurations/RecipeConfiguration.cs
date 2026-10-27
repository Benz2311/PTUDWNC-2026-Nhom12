using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class RecipeConfiguration
    : IEntityTypeConfiguration<Recipe>
{
    public void Configure(
        EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(r => r.Description)
            .HasColumnType("text");

        // PostgreSQL không tự sinh rowversion như SQL Server.
        // RowVersion được ứng dụng gán giá trị và dùng làm Concurrency Token cho Optimistic Concurrency.
        builder.Property(r => r.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();



        builder.HasIndex(r => r.Slug)
            .IsUnique();

        builder.HasIndex(r => r.CategoryId);

        builder.HasIndex(r => r.Status);

        builder.HasIndex(r => r.CreatedAt);

        builder.HasIndex(r => new { r.IsDeleted, r.Status, r.CreatedAt });

        builder.HasIndex(r => new { r.CategoryId, r.IsDeleted, r.Status, r.CreatedAt });

        builder.HasIndex(r => r.Title)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(r => r.Description)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}