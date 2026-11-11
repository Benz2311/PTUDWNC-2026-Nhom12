using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class PersonalRecipeNoteConfiguration : IEntityTypeConfiguration<PersonalRecipeNote>
{
    public void Configure(EntityTypeBuilder<PersonalRecipeNote> builder)
    {
        builder.ToTable("PersonalRecipeNotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(5000);
        builder.HasIndex(x => new { x.RecipeId, x.UserId }).IsUnique();
        builder.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

