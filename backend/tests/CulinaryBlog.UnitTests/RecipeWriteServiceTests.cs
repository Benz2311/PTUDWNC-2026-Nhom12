using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;

namespace CulinaryBlog.UnitTests;

public sealed class RecipeWriteServiceTests
{
    [Fact]
    public void Recipe_CanBeCreatedWithCoreProperties()
    {
        var recipe = new Recipe
        {
            Title = "Bún chả",
            Slug = "bun-cha",
            Status = RecipeStatus.Draft
        };

        Assert.Equal("Bún chả", recipe.Title);
        Assert.Equal("bun-cha", recipe.Slug);
        Assert.Equal(RecipeStatus.Draft, recipe.Status);
    }

    [Fact]
    public void RecipeIngredient_TracksQuantityAndName()
    {
        var ingredient = new RecipeIngredient
        {
            Name = "Bún",
            Quantity = 250m,
            Unit = "g"
        };

        Assert.Equal("Bún", ingredient.Name);
        Assert.Equal(250m, ingredient.Quantity);
        Assert.Equal("g", ingredient.Unit);
    }

    [Fact]
    public void RecipeStep_TracksSequenceAndDescription()
    {
        var step = new RecipeStep
        {
            StepNumber = 2,
            Title = "Xào",
            Description = "Xào thức ăn trên chảo."
        };

        Assert.Equal(2, step.StepNumber);
        Assert.Equal("Xào", step.Title);
        Assert.Contains("Xào", step.Description);
    }
}
