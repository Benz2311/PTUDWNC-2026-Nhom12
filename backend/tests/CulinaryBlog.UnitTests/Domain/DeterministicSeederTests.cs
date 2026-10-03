using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.UnitTests.Domain;

public class DeterministicSeederTests
{
    private sealed class TestRecipe : Recipe
    {
        public TestRecipe(Guid id, DateTime createdAt)
        {
            Id = id;
            CreatedAt = createdAt;
        }
    }

    private static List<Recipe> CreateSampleRecipes(int count = 5)
    {
        var list = new List<Recipe>();
        for (var i = 1; i <= count; i++)
        {
            var recipeId = Guid.Parse($"00000000-0000-0000-0000-00000000000{i:D1}");
            var recipe = new TestRecipe(
                recipeId,
                new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc))
            {
                Title = $"Món ăn thử nghiệm {i}",
                Slug = $"mon-an-thu-nghiem-{i}",
                Description = $"Mô tả món ăn thử nghiệm {i}",
                Content = $"Nội dung món ăn {i}",
                PrepTimeMinutes = 15,
                CookTimeMinutes = 30,
                Servings = 4,
                Difficulty = DifficultyLevel.Medium,
                Status = RecipeStatus.Published
            };

            recipe.Ingredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Thịt bò",
                Quantity = 500,
                Unit = "g",
                SortOrder = 1,
                IsDeleted = false
            });

            recipe.Ingredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = "Hành tây",
                Quantity = 1,
                Unit = "củ",
                SortOrder = 2,
                IsDeleted = false
            });

            list.Add(recipe);
        }

        return list;
    }

    [Fact]
    public void RecipeStepSeeder_ShouldProduceDeterministicResults_AcrossMultipleRuns()
    {
        // Arrange
        var recipes1 = CreateSampleRecipes(5);
        var recipes2 = CreateSampleRecipes(5);

        // Act
        var run1 = RecipeStepSeeder.Generate(recipes1);
        var run2 = RecipeStepSeeder.Generate(recipes2);

        // Assert
        run1.Should().NotBeEmpty();
        run2.Should().NotBeEmpty();
        run1.Count.Should().Be(run2.Count);

        for (var i = 0; i < run1.Count; i++)
        {
            run1[i].Id.Should().Be(run2[i].Id);
            run1[i].RecipeId.Should().Be(run2[i].RecipeId);
            run1[i].StepNumber.Should().Be(run2[i].StepNumber);
            run1[i].Title.Should().Be(run2[i].Title);
            run1[i].Description.Should().Be(run2[i].Description);
            run1[i].CreatedAt.Should().Be(run2[i].CreatedAt);
        }
    }

    [Fact]
    public void RecipeStepSeeder_ShouldProduceSequentialStepNumbers_ForEachRecipe()
    {
        // Arrange
        var recipes = CreateSampleRecipes(3);

        // Act
        var steps = RecipeStepSeeder.Generate(recipes);

        // Assert
        var grouped = steps.GroupBy(s => s.RecipeId);
        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(s => s.StepNumber).ToList();
            ordered.Count.Should().BeGreaterThanOrEqualTo(6);

            for (var idx = 0; idx < ordered.Count; idx++)
            {
                ordered[idx].StepNumber.Should().Be(idx + 1);
                ordered[idx].Description.Should().NotBeNullOrWhiteSpace();
            }
        }
    }

    [Fact]
    public void RecipeImageSeeder_ShouldProduceDeterministicResults_AcrossMultipleRuns()
    {
        // Arrange
        var recipes1 = CreateSampleRecipes(5);
        var recipes2 = CreateSampleRecipes(5);

        // Act
        var run1 = RecipeImageSeeder.Generate(recipes1);
        var run2 = RecipeImageSeeder.Generate(recipes2);

        // Assert
        run1.Should().NotBeEmpty();
        run2.Should().NotBeEmpty();
        run1.Count.Should().Be(run2.Count);

        for (var i = 0; i < run1.Count; i++)
        {
            run1[i].Id.Should().Be(run2[i].Id);
            run1[i].RecipeId.Should().Be(run2[i].RecipeId);
            run1[i].Url.Should().Be(run2[i].Url);
            run1[i].AltText.Should().Be(run2[i].AltText);
            run1[i].IsPrimary.Should().Be(run2[i].IsPrimary);
            run1[i].SortOrder.Should().Be(run2[i].SortOrder);
            run1[i].CreatedAt.Should().Be(run2[i].CreatedAt);
        }
    }

    [Fact]
    public void RecipeImageSeeder_ShouldAssignExactlyOnePrimaryImage_ForEachRecipe()
    {
        // Arrange
        var recipes = CreateSampleRecipes(4);

        // Act
        var images = RecipeImageSeeder.Generate(recipes);

        // Assert
        var grouped = images.GroupBy(img => img.RecipeId);
        foreach (var group in grouped)
        {
            var recipeImages = group.ToList();
            recipeImages.Count.Should().BeInRange(2, 5);

            var primaryImages = recipeImages.Where(img => img.IsPrimary).ToList();
            primaryImages.Should().ContainSingle();
            primaryImages[0].SortOrder.Should().Be(0);
        }
    }
}
