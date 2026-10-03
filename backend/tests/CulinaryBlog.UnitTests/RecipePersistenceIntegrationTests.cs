using System.Data.Common;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace CulinaryBlog.UnitTests;

public sealed class RecipePersistenceIntegrationTests
{
    [PostgreSqlFact]
    public async Task RecipeReadQueries_CoverVisibilityStatisticsTrashAndBoundedDetailSql()
    {
        var connectionString = Environment.GetEnvironmentVariable("CULINARYBLOG_TEST_CONNECTION")!;
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(connection.Database) ||
            !connection.Database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "CULINARYBLOG_TEST_CONNECTION must target a dedicated database whose name ends with '_test'.");
        }

        var sqlCounter = new SelectQueryCounter();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(sqlCounter)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var author = CreateAuthor("recipe-read-author");
        var otherAuthor = CreateAuthor("recipe-read-other");
        var category = Category.Create(
            $"Recipe read {Guid.NewGuid():N}",
            $"recipe-read-{Guid.NewGuid():N}");
        db.Users.AddRange(author, otherAuthor);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var recipeRepository = new CulinaryBlog.Infrastructure.Persistence.Repositories.RecipeRepository(db);
        var categoryRepository = new CulinaryBlog.Infrastructure.Persistence.Repositories.CategoryRepository(db);
        var baselineStatistics = await categoryRepository.GetCategoryStatisticsAsync();

        var recipePrefix = $"recipe-read-{Guid.NewGuid():N}";
        var firstPublished = CreateRecipe(
            "A published",
            $"{recipePrefix}-published-a",
            RecipeStatus.Published,
            author,
            category);
        var secondPublished = CreateRecipe(
            "B published",
            $"{recipePrefix}-published-b",
            RecipeStatus.Published,
            author,
            category);
        var ownedDraft = CreateRecipe(
            "Owned draft",
            $"{recipePrefix}-draft-owned",
            RecipeStatus.Draft,
            author,
            category);
        var foreignDraft = CreateRecipe(
            "Foreign draft",
            $"{recipePrefix}-draft-foreign",
            RecipeStatus.Draft,
            otherAuthor,
            category);
        var archived = CreateRecipe(
            "Archived",
            $"{recipePrefix}-archived",
            RecipeStatus.Archived,
            author,
            category);
        var deleted = CreateRecipe(
            "Deleted published",
            $"{recipePrefix}-deleted",
            RecipeStatus.Published,
            author,
            category);
        deleted.SoftDelete();

        db.Recipes.AddRange(
            firstPublished,
            secondPublished,
            ownedDraft,
            foreignDraft,
            archived,
            deleted);

        for (var index = 0; index < 8; index++)
        {
            firstPublished.Steps.Add(new RecipeStep
            {
                StepNumber = 8 - index,
                Title = $"Step {index}",
                Description = "Prepare the recipe."
            });
            firstPublished.Ingredients.Add(new RecipeIngredient
            {
                Name = $"Ingredient {index}",
                SortOrder = 8 - index
            });
            firstPublished.Images.Add(new RecipeImage
            {
                OriginalUrl = $"https://example.test/{index}.jpg",
                SortOrder = 8 - index
            });
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var publicPage = await recipeRepository.GetListAsync(
            2,
            1,
            new RecipeListOptions(
                CategoryId: category.Id,
                Difficulty: DifficultyLevel.Easy,
                SortBy: RecipeSortField.Title,
                SortDescending: false));
        Assert.Equal(2, publicPage.TotalCount);
        Assert.Equal("B published", Assert.Single(publicPage.Items).Title);

        var authorDrafts = await recipeRepository.GetListAsync(
            1,
            12,
            new RecipeListOptions(
                CategoryId: category.Id,
                AuthorId: author.Id,
                Status: RecipeStatus.Draft));
        Assert.Equal(1, authorDrafts.TotalCount);
        Assert.Equal(ownedDraft.Slug, Assert.Single(authorDrafts.Items).Slug);

        var categoryRecipes = await recipeRepository.GetByCategorySlugAsync(
            category.Slug,
            1,
            12,
            author.Id);
        Assert.NotNull(categoryRecipes);
        Assert.Equal(3, categoryRecipes!.Recipes.TotalCount);
        Assert.DoesNotContain(categoryRecipes.Recipes.Items, item => item.Slug == foreignDraft.Slug);
        Assert.DoesNotContain(categoryRecipes.Recipes.Items, item => item.Slug == archived.Slug);

        var trash = await recipeRepository.GetDeletedAsync(1, 100);
        Assert.Contains(trash.Items, item => item.Slug == deleted.Slug && item.DeletedAt.HasValue);
        Assert.DoesNotContain(trash.Items, item => item.Slug == firstPublished.Slug);

        var categoryStatistics = await categoryRepository.GetCategoryStatisticsAsync();
        Assert.Equal(baselineStatistics.TotalCategories, categoryStatistics.TotalCategories);
        Assert.Equal(baselineStatistics.TotalRecipes + 5, categoryStatistics.TotalRecipes);
        Assert.Equal(baselineStatistics.PublishedRecipes + 2, categoryStatistics.PublishedRecipes);
        Assert.Equal(baselineStatistics.DraftRecipes + 2, categoryStatistics.DraftRecipes);
        Assert.Equal(baselineStatistics.ArchivedRecipes + 1, categoryStatistics.ArchivedRecipes);
        var categoryStatistic = Assert.Single(
            categoryStatistics.Categories,
            item => item.CategoryId == category.Id);
        Assert.Equal(5, categoryStatistic.RecipeCount);
        Assert.Equal(
            Math.Round(5m * 100m / categoryStatistics.TotalRecipes, 2),
            categoryStatistic.Percentage);
        Assert.Equal(categoryStatistics.TotalRecipes, categoryStatistics.RecipesByMonth.Sum(item => item.RecipeCount));

        var detailHandler = new GetRecipeBySlugHandler(db);
        sqlCounter.Reset();
        var detail = await detailHandler.Handle(
            new GetRecipeBySlugQuery(firstPublished.Slug),
            CancellationToken.None);
        Assert.Equal(category.Id, detail.Category.Id);
        Assert.Equal(author.Id, detail.Author.Id);
        Assert.NotNull(detail.Nutrition);
        Assert.Equal(8, detail.Steps.Count);
        Assert.Equal(8, detail.Ingredients.Count);
        Assert.Equal(8, detail.Images.Count);
        Assert.Equal(detail.Steps.OrderBy(item => item.StepNumber).Select(item => item.Id), detail.Steps.Select(item => item.Id));
        Assert.Equal(detail.Ingredients.OrderBy(item => item.SortOrder).Select(item => item.Id), detail.Ingredients.Select(item => item.Id));
        Assert.Equal(detail.Images.OrderBy(item => item.OrderIndex).Select(item => item.Id), detail.Images.Select(item => item.Id));
        Assert.InRange(sqlCounter.SelectCommandCount, 1, 6);

        await Assert.ThrowsAsync<ForbiddenException>(() => detailHandler.Handle(
            new GetRecipeBySlugQuery(ownedDraft.Slug, Guid.NewGuid()),
            CancellationToken.None));
        var ownerDraft = await detailHandler.Handle(
            new GetRecipeBySlugQuery(ownedDraft.Slug, author.Id),
            CancellationToken.None);
        Assert.Equal(ownedDraft.Id, ownerDraft.Id);
        var adminArchived = await detailHandler.Handle(
            new GetRecipeBySlugQuery(archived.Slug, IsAdmin: true),
            CancellationToken.None);
        Assert.Equal(archived.Id, adminArchived.Id);

        await transaction.RollbackAsync();
    }

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

    private static ApplicationUser CreateAuthor(string prefix)
    {
        return new ApplicationUser
        {
            FullName = prefix,
            UserName = $"{prefix}-{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@recipe-read.local",
            PasswordHash = "integration-test-hash",
            EmailConfirmed = true,
            Roles = ["Author"]
        };
    }

    private static Recipe CreateRecipe(
        string title,
        string slug,
        RecipeStatus status,
        ApplicationUser author,
        Category category)
    {
        return new Recipe
        {
            AuthorId = author.Id,
            CategoryId = category.Id,
            Title = title,
            Slug = slug,
            Description = $"{title} description",
            Content = $"{title} content",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 15,
            Servings = 2,
            Difficulty = DifficultyLevel.Easy,
            Status = status,
            PublishedAt = status == RecipeStatus.Published ? DateTime.UtcNow : null,
            Nutrition = new RecipeNutrition { Calories = 100m, Protein = 10m }
        };
    }

    private sealed class SelectQueryCounter : DbCommandInterceptor
    {
        private int _selectCommandCount;

        public int SelectCommandCount => Volatile.Read(ref _selectCommandCount);

        public void Reset()
        {
            Interlocked.Exchange(ref _selectCommandCount, 0);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _selectCommandCount);
            }

            return ValueTask.FromResult(result);
        }
    }
}