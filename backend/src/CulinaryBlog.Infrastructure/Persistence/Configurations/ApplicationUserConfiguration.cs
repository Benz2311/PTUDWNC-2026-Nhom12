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

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => x.UserName)
            .IsUnique();

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.PasswordHash)
            .HasColumnType("text");

        builder.Property(x => x.FullName)
            .HasColumnName("DisplayName")
            .IsRequired()
            .HasMaxLength(100);

        builder.Ignore(x => x.DisplayName);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Bio)
            .HasMaxLength(1000);

        builder.Property(x => x.EmailConfirmed)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .ValueGeneratedNever();

        builder.HasIndex(x => x.IsActive);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // FullName maps to the DisplayName database column; DisplayName is a compatibility alias.

        // Roles được lưu trữ dưới dạng mảng PostgreSQL (text[]) thay vì qua bảng
        // IdentityRole, phù hợp với thiết kế role in-memory -> JWT tại AuthService.
        builder.Property(x => x.Roles)
            .HasColumnName("Roles")
            .HasColumnType("text[]")
            .IsRequired();

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
