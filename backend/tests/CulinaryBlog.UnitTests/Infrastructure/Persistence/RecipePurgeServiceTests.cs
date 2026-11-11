using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CulinaryBlog.UnitTests.Infrastructure.Persistence;

public sealed class RecipePurgeServiceTests
{
    [Fact]
    public async Task PurgeAsync_RemovesRecipeReferencesAndPreservesShoppingListItems()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"recipe-purge-{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);
        var recipeId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = recipeId,
            AuthorId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Title = "Deleted recipe",
            Slug = "deleted-recipe",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        var shoppingListItem = new ShoppingListItem
        {
            RecipeId = recipeId,
            UserId = Guid.NewGuid(),
            Name = "Ingredient"
        };
        db.Recipes.Add(recipe);
        db.RecipeComments.Add(new RecipeComment { RecipeId = recipeId, UserId = Guid.NewGuid(), Content = "Comment" });
        db.PersonalRecipeNotes.Add(new PersonalRecipeNote { RecipeId = recipeId, UserId = Guid.NewGuid(), Content = "Note" });
        db.RecipeRevisions.Add(new RecipeRevision
        {
            RecipeId = recipeId,
            EditorId = Guid.NewGuid(),
            Version = 1,
            Title = recipe.Title,
            Description = string.Empty,
            ContentJson = "{}",
            ChangeSummary = "Created"
        });
        db.MealPlanEntries.Add(new MealPlanEntry { RecipeId = recipeId, UserId = Guid.NewGuid() });
        db.ShoppingListItems.Add(shoppingListItem);
        await db.SaveChangesAsync();

        var cacheInvalidator = new Mock<IRecipeCacheInvalidator>();
        var service = new RecipePurgeService(
            db,
            new ConfigurationBuilder().Build(),
            cacheInvalidator.Object);

        var purged = await service.PurgeAsync(recipeId, Guid.NewGuid());

        Assert.True(purged);
        Assert.False(await db.Recipes.IgnoreQueryFilters().AnyAsync(item => item.Id == recipeId));
        Assert.Empty(await db.RecipeComments.IgnoreQueryFilters().Where(item => item.RecipeId == recipeId).ToListAsync());
        Assert.Empty(await db.PersonalRecipeNotes.Where(item => item.RecipeId == recipeId).ToListAsync());
        Assert.Empty(await db.RecipeRevisions.Where(item => item.RecipeId == recipeId).ToListAsync());
        Assert.Empty(await db.MealPlanEntries.IgnoreQueryFilters().Where(item => item.RecipeId == recipeId).ToListAsync());
        Assert.Null((await db.ShoppingListItems.IgnoreQueryFilters().SingleAsync()).RecipeId);
        Assert.Contains(await db.RecipeAuditLogs.ToListAsync(), item => item.RecipeId == recipeId && item.Action == "Purge");
        cacheInvalidator.Verify(
            item => item.InvalidateAsync(recipeId, recipe.Slug, recipe.CategoryId, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PurgeAsync_ReturnsFalseForRecipeThatIsNotSoftDeleted()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"recipe-purge-active-{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);
        var recipe = new Recipe
        {
            AuthorId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Title = "Active recipe",
            Slug = "active-recipe"
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var service = new RecipePurgeService(
            db,
            new ConfigurationBuilder().Build(),
            Mock.Of<IRecipeCacheInvalidator>());

        Assert.False(await service.PurgeAsync(recipe.Id, Guid.NewGuid()));
        Assert.True(await db.Recipes.AnyAsync(item => item.Id == recipe.Id));
    }
}
