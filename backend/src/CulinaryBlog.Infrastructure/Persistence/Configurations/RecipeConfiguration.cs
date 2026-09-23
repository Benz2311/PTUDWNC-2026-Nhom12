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

        builder.Property(r => r.Difficulty)
            .HasConversion<string>();

        builder.Property(r => r.Status)
            .HasConversion<string>();

        builder.HasIndex(r => r.CategoryId);

        builder.HasIndex(r => r.Status);

        builder.HasIndex(r => r.CreatedAt);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}