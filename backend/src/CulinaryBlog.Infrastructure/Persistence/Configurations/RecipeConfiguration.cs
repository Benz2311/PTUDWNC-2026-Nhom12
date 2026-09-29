using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");

        builder.Property(recipe => recipe.AuthorId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasOne(recipe => recipe.Author)
            .WithMany(user => user.Recipes)
            .HasForeignKey(recipe => recipe.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
