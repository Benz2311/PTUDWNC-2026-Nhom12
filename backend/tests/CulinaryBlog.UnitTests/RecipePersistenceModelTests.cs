using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CulinaryBlog.UnitTests;

public sealed class RecipePersistenceModelTests
{
    [Fact]
    public void RecipeSearchIndexes_UsePostgresTrigramGin()
    {
        using var context = CreateContext();
        var recipe = context.Model.FindEntityType(typeof(Recipe))!;
        var titleIndex = FindIndex(recipe, nameof(Recipe.Title));
        var descriptionIndex = FindIndex(recipe, nameof(Recipe.Description));

        Assert.Equal("gin", titleIndex.FindAnnotation("Npgsql:IndexMethod")?.Value);
        Assert.Equal("gin", descriptionIndex.FindAnnotation("Npgsql:IndexMethod")?.Value);
    }

    [Fact]
    public void RecipeListIndexes_CoverStatusDateAndCategoryFilters()
    {
        using var context = CreateContext();
        var recipe = context.Model.FindEntityType(typeof(Recipe))!;

        AssertIndexColumns(recipe, nameof(Recipe.IsDeleted), nameof(Recipe.Status), nameof(Recipe.CreatedAt));
        AssertIndexColumns(recipe, nameof(Recipe.CategoryId), nameof(Recipe.IsDeleted), nameof(Recipe.Status), nameof(Recipe.CreatedAt));
    }

    [Fact]
    public void ChildIndexes_CoverRecipeOrderingAndUniqueStepNumbers()
    {
        using var context = CreateContext();
        var steps = context.Model.FindEntityType(typeof(RecipeStep))!;
        var images = context.Model.FindEntityType(typeof(RecipeImage))!;

        var stepIndex = FindIndex(steps, nameof(RecipeStep.RecipeId), nameof(RecipeStep.StepNumber));
        var imageIndex = FindIndex(images, nameof(RecipeImage.RecipeId), nameof(RecipeImage.SortOrder));

        Assert.True(stepIndex.IsUnique);
        Assert.False(imageIndex.IsUnique);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IIndex FindIndex(IEntityType entityType, params string[] propertyNames) =>
        entityType.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

    private static void AssertIndexColumns(IEntityType entityType, params string[] propertyNames)
    {
        var index = FindIndex(entityType, propertyNames);
        Assert.Equal(propertyNames, index.Properties.Select(property => property.Name));
    }
}
