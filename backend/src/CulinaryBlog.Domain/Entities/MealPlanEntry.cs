using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

public class MealPlanEntry : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public DateOnly PlannedDate { get; set; }
    public string MealType { get; set; } = "Lunch";
}
