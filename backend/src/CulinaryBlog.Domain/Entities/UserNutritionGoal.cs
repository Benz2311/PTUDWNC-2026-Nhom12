using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Domain.Entities;

public class UserNutritionGoal
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int Age { get; set; }
    public NutritionGender Gender { get; set; }
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public NutritionActivityLevel ActivityLevel { get; set; }
    public NutritionGoalType GoalType { get; set; }
    public double TargetCalories { get; set; }
    public double TargetProteinG { get; set; }
    public double TargetCarbG { get; set; }
    public double TargetFatG { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
