using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.ToTable("UserFollows", table =>
            table.HasCheckConstraint("CK_UserFollows_NoSelfFollow", "\"FollowerId\" <> \"FollowingId\""));
        builder.HasKey(x => new { x.FollowerId, x.FollowingId });
        builder.HasIndex(x => new { x.FollowingId, x.CreatedAt });
        builder.HasOne(x => x.Follower).WithMany().HasForeignKey(x => x.FollowerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Following).WithMany().HasForeignKey(x => x.FollowingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActivityType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => x.TargetId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserNutritionGoalConfiguration : IEntityTypeConfiguration<UserNutritionGoal>
{
    public void Configure(EntityTypeBuilder<UserNutritionGoal> builder)
    {
        builder.ToTable("UserNutritionGoals");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ActivityLevel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.GoalType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.HeightCm).HasPrecision(6, 2);
        builder.Property(x => x.WeightKg).HasPrecision(6, 2);
        builder.Property(x => x.TargetCalories).HasPrecision(8, 2);
        builder.Property(x => x.TargetProteinG).HasPrecision(8, 2);
        builder.Property(x => x.TargetCarbG).HasPrecision(8, 2);
        builder.Property(x => x.TargetFatG).HasPrecision(8, 2);
        builder.HasOne(x => x.User).WithOne().HasForeignKey<UserNutritionGoal>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
