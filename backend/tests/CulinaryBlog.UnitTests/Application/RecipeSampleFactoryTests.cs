using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using FluentAssertions;

namespace CulinaryBlog.UnitTests.Application;

public class RecipeSampleFactoryTests
{
    [Fact]
    public void Create_ReturnsRecipeWithAllRequiredComponents()
    {
        var authorId = Guid.NewGuid();
        var category = Category.Create("Món Việt", "mon-viet", "Món Việt gia đình.");

        var recipe = RecipeSampleFactory.Create(authorId, category);

        recipe.AuthorId.Should().Be(authorId);
        recipe.CategoryId.Should().Be(category.Id);
        recipe.Title.Should().NotBeNullOrWhiteSpace();
        recipe.Slug.Should().NotBeNullOrWhiteSpace();
        recipe.Description.Should().NotBeNullOrWhiteSpace();
        recipe.Content.Should().NotBeNullOrWhiteSpace();
        recipe.PublishedAt.Should().NotBeNull();
        recipe.PublishedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        recipe.PrepTimeMinutes.Should().BePositive();
        recipe.CookTimeMinutes.Should().BePositive();
        recipe.Servings.Should().BePositive();
        recipe.Status.Should().Be(RecipeStatus.Published);

        recipe.Ingredients.Should().HaveCountGreaterThanOrEqualTo(10);
        recipe.Ingredients.Should().OnlyContain(ingredient =>
            !string.IsNullOrWhiteSpace(ingredient.Name) &&
            ingredient.Quantity.HasValue && ingredient.Quantity.Value > 0 &&
            !string.IsNullOrWhiteSpace(ingredient.Unit));

        recipe.Nutrition.Calories.Should().NotBeNull();
        recipe.Nutrition.Protein.Should().NotBeNull();
        recipe.Nutrition.Carbohydrates.Should().NotBeNull();
        recipe.Nutrition.Fat.Should().NotBeNull();
        recipe.Nutrition.Fiber.Should().NotBeNull();
        recipe.Nutrition.Sodium.Should().NotBeNull();

        recipe.Steps.Should().HaveCountGreaterThanOrEqualTo(5);
        recipe.Steps.Should().OnlyContain(step =>
            !string.IsNullOrWhiteSpace(step.Title) &&
            !string.IsNullOrWhiteSpace(step.Description));

        recipe.Images.Should().NotBeEmpty();
        recipe.Images.Should().ContainSingle(image => image.IsPrimary);
        recipe.Images.Should().OnlyContain(image => !string.IsNullOrWhiteSpace(image.Url));
    }

    [Fact]
    public void Create_GeneratesDistinctRecipeSlugs()
    {
        var category = Category.Create("Món Việt", "mon-viet", "Món Việt gia đình.");

        var first = RecipeSampleFactory.Create(Guid.NewGuid(), category);
        var second = RecipeSampleFactory.Create(Guid.NewGuid(), category);

        first.Slug.Should().NotBe(second.Slug);
    }
}