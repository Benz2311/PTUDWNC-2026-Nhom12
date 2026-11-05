using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeRevisionConfiguration : IEntityTypeConfiguration<RecipeRevision>
{
    public void Configure(EntityTypeBuilder<RecipeRevision> builder)
    {
        builder.ToTable("RecipeRevisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.ContentJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.ChangeSummary).IsRequired().HasMaxLength(500);
        builder.HasIndex(x => new { x.RecipeId, x.Version }).IsUnique();
        builder.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Editor).WithMany().HasForeignKey(x => x.EditorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ModerationLogConfiguration : IEntityTypeConfiguration<ModerationLog>
{
    public void Configure(EntityTypeBuilder<ModerationLog> builder)
    {
        builder.ToTable("ModerationLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TargetType).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.HasIndex(x => new { x.TargetType, x.TargetId, x.CreatedAt });
        builder.HasOne(x => x.Moderator).WithMany().HasForeignKey(x => x.ModeratorId).OnDelete(DeleteBehavior.Restrict);
    }
}
