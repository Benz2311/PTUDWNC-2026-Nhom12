using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .HasColumnName("DisplayName")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.UserName)
            .HasMaxLength(256);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.EmailConfirmed)
            .IsRequired();

        builder.Property(x => x.Roles)
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(x => x.PasswordHash)
            .HasColumnType("text");

        builder.HasIndex(x => x.UserName)
            .IsUnique();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasMany(x => x.RefreshTokens)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Recipes)
            .WithOne(x => x.Author)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}