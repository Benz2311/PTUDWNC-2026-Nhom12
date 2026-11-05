using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Domain.Enums;
using FluentAssertions;

namespace CulinaryBlog.UnitTests.Application.Common;

public class NutritionGoalCalculatorTests
{
    [Fact]
    public void Calculate_UsesMifflinStJeorAndModerateActivityMultiplier()
    {
        var result = NutritionGoalCalculator.Calculate(
            age: 30,
            gender: NutritionGender.Male,
            heightCm: 180,
            weightKg: 80,
            activityLevel: NutritionActivityLevel.Moderate,
            goalType: NutritionGoalType.WeightLoss);

        result.BasalMetabolicRate.Should().Be(1780);
        result.TotalDailyEnergyExpenditure.Should().Be(2759);
        result.Calories.Should().Be(2259);
        result.ProteinG.Should().Be(128);
    }

    [Theory]
    [InlineData(12, NutritionGender.Female, 165, 60, NutritionActivityLevel.Light, NutritionGoalType.Maintenance)]
    [InlineData(30, (NutritionGender)99, 165, 60, NutritionActivityLevel.Light, NutritionGoalType.Maintenance)]
    [InlineData(30, NutritionGender.Female, 165, 60, (NutritionActivityLevel)99, NutritionGoalType.Maintenance)]
    [InlineData(30, NutritionGender.Female, 165, 60, NutritionActivityLevel.Light, (NutritionGoalType)99)]
    public void Calculate_RejectsInvalidInputs(
        int age,
        NutritionGender gender,
        double heightCm,
        double weightKg,
        NutritionActivityLevel activityLevel,
        NutritionGoalType goalType)
    {
        var act = () => NutritionGoalCalculator.Calculate(age, gender, heightCm, weightKg, activityLevel, goalType);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
