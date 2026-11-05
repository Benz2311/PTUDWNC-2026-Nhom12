

using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Common.Utilities;

public static class NutritionGoalCalculator
{
    public static NutritionTargets Calculate(
        int age,
        NutritionGender gender,
        double heightCm,
        double weightKg,
        NutritionActivityLevel activityLevel,
        NutritionGoalType goalType)
    {
        if (age is < 13 or > 120)
            throw new ArgumentOutOfRangeException(nameof(age), "Age must be between 13 and 120.");
        if (heightCm is < 100 or > 250 || !double.IsFinite(heightCm))
            throw new ArgumentOutOfRangeException(nameof(heightCm), "Height must be between 100 and 250 cm.");
        if (weightKg is < 30 or > 350 || !double.IsFinite(weightKg))
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Weight must be between 30 and 350 kg.");
        if (!Enum.IsDefined(gender))
            throw new ArgumentOutOfRangeException(nameof(gender), "Gender is invalid.");

        var bmr = (10 * weightKg) + (6.25 * heightCm) - (5 * age) +
            (gender == NutritionGender.Male ? 5 : -161);
        var activityMultiplier = activityLevel switch
        {
            NutritionActivityLevel.Sedentary => 1.2,
            NutritionActivityLevel.Light => 1.375,
            NutritionActivityLevel.Moderate => 1.55,
            NutritionActivityLevel.Active => 1.725,
            _ => throw new ArgumentOutOfRangeException(nameof(activityLevel))
        };
        var goalAdjustment = goalType switch
        {
            NutritionGoalType.WeightLoss => -500,
            NutritionGoalType.Maintenance => 0,
            NutritionGoalType.MuscleGain => 250,
            _ => throw new ArgumentOutOfRangeException(nameof(goalType))
        };
        var tdee = bmr * activityMultiplier;
        var calories = Math.Max(bmr, tdee + goalAdjustment);
        var protein = weightKg * 1.6;
        var fat = calories * 0.25 / 9;
        var carbohydrates = Math.Max(0, (calories - protein * 4 - fat * 9) / 4);

        return new NutritionTargets(
            Math.Round(bmr, 0),
            Math.Round(tdee, 0),
            Math.Round(calories, 0),
            Math.Round(protein, 1),
            Math.Round(carbohydrates, 1),
            Math.Round(fat, 1));
    }
}

public sealed record NutritionTargets(
    double BasalMetabolicRate,
    double TotalDailyEnergyExpenditure,
    double Calories,
    double ProteinG,
    double CarbohydratesG,
    double FatG);
