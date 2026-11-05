using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
    public void Configure(EntityTypeBuilder<ShoppingList> builder)
    {
        builder.ToTable("ShoppingLists");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ShoppingListItemConfigurationV2 : IEntityTypeConfiguration<ShoppingListItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
    {
        builder.Property(x => x.ShoppingListId);
        builder.Property(x => x.Category).HasMaxLength(80);
        builder.Property(x => x.SortOrder).IsRequired();
        builder.HasIndex(x => new { x.ShoppingListId, x.SortOrder });
        builder.HasOne(x => x.ShoppingList).WithMany(x => x.Items)
            .HasForeignKey(x => x.ShoppingListId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.ToTable("MealPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => new { x.UserId, x.StartDate, x.EndDate });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MealPlanItemConfiguration : IEntityTypeConfiguration<MealPlanItem>
{
    public void Configure(EntityTypeBuilder<MealPlanItem> builder)
    {
        builder.ToTable("MealPlanItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MealType).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Servings).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.Consumed).HasDefaultValue(false);
        builder.HasIndex(x => new { x.MealPlanId, x.PlanDate, x.MealType }).IsUnique();
        builder.HasIndex(x => new { x.MealPlanId, x.ConsumedAt }).HasFilter("\"Consumed\" = true");
        builder.HasOne(x => x.MealPlan).WithMany(x => x.Items).HasForeignKey(x => x.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
