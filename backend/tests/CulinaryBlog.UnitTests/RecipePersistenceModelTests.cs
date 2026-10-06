using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Api.Endpoints.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Reflection;

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

    [Fact]
    public void PublishGuard_RejectsUnconfirmedAuthor()
    {
        var method = typeof(RecipeEndpoints).GetMethod("CanPublishRecipe", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var recipe = new Recipe
        {
            Status = RecipeStatus.Draft,
            Ingredients = [new RecipeIngredient { Name = "Salt", Quantity = 1m, SortOrder = 0 }],
            Steps = [new RecipeStep { StepNumber = 1, Title = "Mix", Description = "Mix ingredients" }],
            Category = Category.Create("Dinner", "dinner")
        };
        var author = new ApplicationUser { EmailConfirmed = false };

        var result = (bool)method.Invoke(null, [recipe, author])!;

        Assert.False(result);
    }

    [Fact]
    public void IfMatchGuard_RejectsStaleVersion()
    {
        var method = typeof(RecipeEndpoints).GetMethod("MatchesIfMatchHeader", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var recipe = new Recipe { xmin = 42 };
        var result = (bool)method.Invoke(null, [recipe, "\"41\""])!;

        Assert.False(result);
    }

    [Fact]
    public void RecipeEtag_IsEmittedAsStrongXminTag()
    {
        var method = typeof(RecipeEndpoints).GetMethod("SetRecipeEtag", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var context = new DefaultHttpContext();

        method.Invoke(null, [context, new Recipe { xmin = 42 }]);

        Assert.Equal("\"42\"", context.Response.Headers.ETag.ToString());
    }

    [Fact]
    public void PublishGuard_RequiresConfirmedAuthorEvenForAdmin()
    {
        var method = typeof(RecipeEndpoints).GetMethod("CanPublishRecipe", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var recipe = new Recipe
        {
            Title = "A complete recipe",
            Content = "Mix, cook, and serve.",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 15,
            Servings = 2,
            Difficulty = DifficultyLevel.Easy,
            Category = Category.Create("Dinner", "dinner"),
            Ingredients = [new RecipeIngredient { Name = "Salt", Quantity = 1m }],
            Steps = [new RecipeStep { StepNumber = 1, Description = "Mix and cook." }]
        };

        var result = (bool)method.Invoke(null, [recipe, new ApplicationUser { EmailConfirmed = true }])!;
        var unconfirmedAuthorResult = (bool)method.Invoke(null, [recipe, new ApplicationUser { EmailConfirmed = false }])!;

        Assert.True(result);
        Assert.False(unconfirmedAuthorResult);
    }

    [Fact]
    public void IngredientQuantity_RejectsFractionStringsButAcceptsJsonNumbersAndNull()
    {
        var method = typeof(RecipeEndpoints).GetMethod("ValidateIngredient", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        using var fractionDocument = JsonDocument.Parse("\"1/2\"");
        using var decimalDocument = JsonDocument.Parse("0.5");
        using var nullDocument = JsonDocument.Parse("null");

        var fractionError = (string?)method.Invoke(null, [new IngredientRequest("Salt", fractionDocument.RootElement, null)]);
        var decimalError = (string?)method.Invoke(null, [new IngredientRequest("Salt", decimalDocument.RootElement, null)]);
        var nullError = (string?)method.Invoke(null, [new IngredientRequest("Salt", nullDocument.RootElement, null)]);
        var negativeOrderError = (string?)method.Invoke(null, [new IngredientRequest("Salt", nullDocument.RootElement, null, OrderIndex: -1)]);

        Assert.NotNull(fractionError);
        Assert.Null(decimalError);
        Assert.Null(nullError);
        Assert.NotNull(negativeOrderError);
    }

    [Fact]
    public void LifecycleEntitiesAndDeletedAt_AreMappedInEfModel()
    {
        using var context = CreateContext();
        var recipe = context.Model.FindEntityType(typeof(Recipe))!;
        var history = context.Model.FindEntityType(typeof(RecipeSlugHistory))!;
        var audit = context.Model.FindEntityType(typeof(RecipeAuditLog))!;
        var historyIndex = FindIndex(history, nameof(RecipeSlugHistory.RecipeId), nameof(RecipeSlugHistory.Slug));

        Assert.NotNull(recipe.FindProperty(nameof(Recipe.DeletedAt)));
        Assert.True(historyIndex.IsUnique);
        Assert.NotNull(audit.FindProperty(nameof(RecipeAuditLog.ActorId)));
    }

    [Fact]
    public void LifecycleMigration_IsDiscoverableByEfCore()
    {
        using var context = CreateContext();

        Assert.Contains("20261003090000_AddRecipeLifecycleSupport", context.Database.GetMigrations());
        Assert.Contains("20261003100000_AddRecipeNutritionSource", context.Database.GetMigrations());
    }

    [Fact]
    public void MigrationSnapshot_MatchesCurrentModel()
    {
        using var context = CreateContext();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var modelDiffer = context.GetService<IMigrationsModelDiffer>();
        var modelInitializer = context.GetService<IModelRuntimeInitializer>();
        var snapshot = Assert.IsAssignableFrom<ModelSnapshot>(migrationsAssembly.ModelSnapshot);
        var snapshotModel = modelInitializer.Initialize(snapshot.Model, designTime: true);
        var currentModel = context.GetService<IDesignTimeModel>().Model;

        var differences = modelDiffer.GetDifferences(
            snapshotModel.GetRelationalModel(),
            currentModel.GetRelationalModel());

        Assert.True(differences.Count == 0, string.Join(", ", differences.Select(operation =>
            $"{operation.GetType().Name} {operation.GetType().GetProperty("Table")?.GetValue(operation)}.{operation.GetType().GetProperty("Name")?.GetValue(operation)}")));
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
