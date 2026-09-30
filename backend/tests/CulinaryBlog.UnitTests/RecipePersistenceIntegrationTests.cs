using CulinaryBlog.Application.Features.Recipes;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.UnitTests;

public sealed class RecipePersistenceIntegrationTests
{
    [PostgreSqlFact]
    public async Task RecipeAndIngredientWrites_PersistRelationsAndOwnedNutrition()
    {
        var connectionString = Environment.GetEnvironmentVariable("CULINARYBLOG_TEST_CONNECTION")!;
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(connection.Database) ||
            !connection.Database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "CULINARYBLOG_TEST_CONNECTION must target a dedicated database whose name ends with '_test'.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            FullName = "Recipe persistence integration test",
            UserName = $"recipe-test-{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@recipe-test.local",
            PasswordHash = "integration-test-hash",
            EmailConfirmed = true,
            Roles = ["Author"]
        };
        var category = Category.Create("Test category", $"test-{Guid.NewGuid():N}");
        db.Users.Add(user);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var recipeRepository = new RecipeRepository(db);
        var unitOfWork = new ApplicationUnitOfWork(db);
        var writeService = new RecipeWriteService(recipeRepository, unitOfWork);
        var recipe = new Recipe
        {
            AuthorId = user.Id,
            CategoryId = category.Id,
            Title = "Integration test recipe",
            Slug = $"integration-test-{Guid.NewGuid():N}",
            Description = "Recipe persistence test",
            Content = "Test instructions",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 0,
            Servings = 2,
            Difficulty = DifficultyLevel.Easy,
            Nutrition = new RecipeNutrition { Calories = 125.5m, Protein = 8.25m }
        };

        await writeService.CreateAsync(recipe);
        var ingredient = new RecipeIngredient
        {
            RecipeId = recipe.Id,
            Name = "Test ingredient",
            Quantity = 2m,
            Unit = "g",
            SortOrder = 0
        };
        await writeService.AddIngredientAsync(ingredient);

        ingredient.Quantity = 3m;
        await writeService.UpdateIngredientAsync(ingredient);

        db.ChangeTracker.Clear();
        var createdRecipe = await db.Recipes
            .AsNoTracking()
            .Include(item => item.Ingredients)
            .SingleAsync(item => item.Id == recipe.Id);
        Assert.Equal(user.Id, createdRecipe.AuthorId);
        Assert.Equal(category.Id, createdRecipe.CategoryId);
        Assert.Equal(125.5m, createdRecipe.Nutrition.Calories);
        Assert.Equal(8.25m, createdRecipe.Nutrition.Protein);
        Assert.Equal(3m, Assert.Single(createdRecipe.Ingredients).Quantity);

        await writeService.DeleteIngredientAsync(ingredient);
        db.ChangeTracker.Clear();
        Assert.False(await db.RecipeIngredients
            .AsNoTracking()
            .AnyAsync(item => item.Id == ingredient.Id));

        var updatedRecipe = await recipeRepository.GetForUpdateAsync(recipe.Id);
        Assert.NotNull(updatedRecipe);
        updatedRecipe!.Title = "Updated integration recipe";
        updatedRecipe.Nutrition.Calories = 210m;
        var replacementIngredient = new RecipeIngredient
        {
            Name = "Replacement ingredient",
            Quantity = 1m,
            Unit = "piece",
            SortOrder = 0
        };
        var step = new RecipeStep
        {
            StepNumber = 1,
            Title = "Prepare",
            Description = "Prepare the test ingredient."
        };
        var image = new RecipeImage
        {
            Url = "https://example.test/recipe.jpg",
            AltText = "Test recipe",
            IsPrimary = true,
            SortOrder = 0
        };

        await writeService.ReplaceContentsAsync(
            updatedRecipe,
            [replacementIngredient],
            [step],
            [image]);

        db.ChangeTracker.Clear();
        var updatedFromDatabase = await db.Recipes
            .AsNoTracking()
            .Include(item => item.Ingredients)
            .Include(item => item.Steps)
            .Include(item => item.Images)
            .SingleAsync(item => item.Id == recipe.Id);
        Assert.Equal("Updated integration recipe", updatedFromDatabase.Title);
        Assert.Equal(210m, updatedFromDatabase.Nutrition.Calories);
        Assert.Equal("Replacement ingredient", Assert.Single(updatedFromDatabase.Ingredients).Name);
        Assert.Equal("Prepare", Assert.Single(updatedFromDatabase.Steps).Title);
        Assert.True(Assert.Single(updatedFromDatabase.Images).IsPrimary);

        var recipeToDelete = await recipeRepository.GetByIdAsync(recipe.Id);
        Assert.NotNull(recipeToDelete);
        await writeService.DeleteAsync(recipeToDelete!);

        db.ChangeTracker.Clear();
        Assert.True(await db.Recipes
            .AsNoTracking()
            .Where(item => item.Id == recipe.Id)
            .Select(item => item.IsDeleted)
            .SingleAsync());

        await transaction.RollbackAsync();
    }

    private sealed class PostgreSqlFactAttribute : FactAttribute
    {
        public PostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CULINARYBLOG_TEST_CONNECTION")))
            {
                Skip = "Set CULINARYBLOG_TEST_CONNECTION to a dedicated PostgreSQL database ending in '_test'.";
            }
        }
    }
}