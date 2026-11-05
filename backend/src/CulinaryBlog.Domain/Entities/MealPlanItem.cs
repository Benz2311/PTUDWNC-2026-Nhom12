namespace CulinaryBlog.Domain.Entities;

public class MealPlanItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MealPlanId { get; set; }
    public MealPlan MealPlan { get; set; } = null!;
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public DateOnly PlanDate { get; set; }
    public string MealType { get; set; } = "Lunch";
    public int Servings { get; set; } = 1;
    public string? Note { get; set; }
    public bool Consumed { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}
