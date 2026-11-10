using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Recipes;

public class GetRecipeBySlugQueryTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetRecipeBySlug_WhenSlugNullOrEmpty_ThrowsNotFoundException(string? slug)
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetRecipeBySlugHandler(db);
        var query = new GetRecipeBySlugQuery(slug!);

        var act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetRecipeBySlug_WhenRecipeDoesNotExist_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var handler = new GetRecipeBySlugHandler(db);
        var query = new GetRecipeBySlugQuery("non-existent-slug");

        var act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetRecipeBySlug_WhenDraftAndUserNotAuthorAndNotAdmin_ThrowsForbiddenException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var authorId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Slug = "draft-recipe",
            Title = "Draft Recipe",
            Status = RecipeStatus.Draft,
            IsDeleted = false
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new GetRecipeBySlugHandler(db);

        // Anonymous user
        var actAnonymous = async () => await handler.Handle(new GetRecipeBySlugQuery("draft-recipe", CurrentUserId: null), CancellationToken.None);
        await actAnonymous.Should().ThrowAsync<ForbiddenException>();

        // Other user
        var actOtherUser = async () => await handler.Handle(new GetRecipeBySlugQuery("draft-recipe", CurrentUserId: Guid.NewGuid()), CancellationToken.None);
        await actOtherUser.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetRecipeBySlug_WhenDraftAndUserIsAuthor_ReturnsRecipeDetail()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var authorId = Guid.NewGuid();
        var author = new ApplicationUser { Id = authorId, DisplayName = "Chef Author", UserName = "chef_auth" };
        var category = Category.Create("Test Cat", "test-cat");
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe
        {
            Id = recipeId,
            AuthorId = authorId,
            Author = author,
            CategoryId = category.Id,
            Category = category,
            Slug = "my-draft-recipe",
            Title = "My Draft Recipe",
            Description = "A great draft",
            Content = "Detailed content",
            Status = RecipeStatus.Draft,
            IsDeleted = false
        };
        db.Users.Add(author);
        db.Categories.Add(category);
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new GetRecipeBySlugHandler(db);
        var query = new GetRecipeBySlugQuery("my-draft-recipe", CurrentUserId: authorId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("My Draft Recipe");
        result.Status.Should().Be(RecipeStatus.Draft);
        result.Author.DisplayName.Should().Be("Chef Author");
        result.Category.Name.Should().Be("Test Cat");
    }

    [Fact]
    public async Task GetRecipeBySlug_WhenDraftAndUserIsAdmin_ReturnsRecipeDetail()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var author = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Chef Author", UserName = "chef_admin_target" };
        var category = Category.Create("Test Cat", "test-cat-admin");
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            AuthorId = author.Id,
            Author = author,
            CategoryId = category.Id,
            Category = category,
            Slug = "admin-can-view-draft",
            Title = "Admin Draft Recipe",
            Status = RecipeStatus.Draft,
            IsDeleted = false
        };
        db.Users.Add(author);
        db.Categories.Add(category);
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new GetRecipeBySlugHandler(db);
        var query = new GetRecipeBySlugQuery("admin-can-view-draft", CurrentUserId: Guid.NewGuid(), IsAdmin: true);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("Admin Draft Recipe");
    }

    [Fact]
    public async Task GetRecipeBySlug_WhenPublished_ReturnsCompleteDetailWithStepsAndNutrition()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var author = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Gordon", UserName = "gordon" };
        var category = Category.Create("European", "european");
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe
        {
            Id = recipeId,
            AuthorId = author.Id,
            Author = author,
            CategoryId = category.Id,
            Category = category,
            Slug = "beef-wellington",
            Title = "Beef Wellington",
            Description = "Classic dish",
            Content = "Full recipe guide",
            PrepTimeMinutes = 30,
            CookTimeMinutes = 45,
            Servings = 4,
            Difficulty = DifficultyLevel.Medium,
            Status = RecipeStatus.Published,
            PublishedAt = DateTime.UtcNow,
            IsDeleted = false,
            Nutrition = new RecipeNutrition
            {
                Calories = 650,
                Protein = 45,
                Carbohydrates = 30,
                Fat = 35,
                Fiber = 3,
                Sodium = 600,
                Source = NutritionSource.Manual
            }
        };
        db.Users.Add(author);
        db.Categories.Add(category);
        db.Recipes.Add(recipe);

        db.RecipeSteps.AddRange(
            new RecipeStep { Id = Guid.NewGuid(), RecipeId = recipeId, StepNumber = 1, Title = "Prep", Description = "Sear the beef", IsDeleted = false },
            new RecipeStep { Id = Guid.NewGuid(), RecipeId = recipeId, StepNumber = 2, Title = "Bake", Description = "Wrap in pastry and bake", IsDeleted = false }
        );

        db.RecipeIngredients.AddRange(
            new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = recipeId, Name = "Beef Tenderloin", Quantity = 1, Unit = "kg", SortOrder = 1, IsDeleted = false },
            new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = recipeId, Name = "Puff Pastry", Quantity = 500, Unit = "g", SortOrder = 2, IsDeleted = false }
        );

        db.RecipeImages.AddRange(
            new RecipeImage { Id = Guid.NewGuid(), RecipeId = recipeId, OriginalUrl = "https://img.local/prim.jpg", MediumUrl = "https://img.local/prim_med.jpg", IsPrimary = true, OrderIndex = 0, IsDeleted = false },
            new RecipeImage { Id = Guid.NewGuid(), RecipeId = recipeId, OriginalUrl = "https://img.local/sec.jpg", IsPrimary = false, OrderIndex = 1, IsDeleted = false }
        );

        await db.SaveChangesAsync();

        var handler = new GetRecipeBySlugHandler(db);
        var query = new GetRecipeBySlugQuery("beef-wellington");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("Beef Wellington");
        result.CoverImageUrl.Should().Be("https://img.local/prim_med.jpg");
        result.Nutrition.Should().NotBeNull();
        result.Nutrition!.Calories.Should().Be(650);
        result.Nutrition.Protein.Should().Be(45);
        result.Steps.Should().HaveCount(2);
        result.Ingredients.Should().HaveCount(2);
        result.Images.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRecipeBySlug_WhenRequestedViaOldSlugHistory_ResolvesAndReturnsRecipe()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var author = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Chef", UserName = "chef_hist" };
        var category = Category.Create("Soup", "soup");
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe
        {
            Id = recipeId,
            AuthorId = author.Id,
            Author = author,
            CategoryId = category.Id,
            Category = category,
            Slug = "pho-bo-ha-noi-dac-biet",
            Title = "Pho Bo Ha Noi",
            Status = RecipeStatus.Published,
            IsDeleted = false
        };
        db.Users.Add(author);
        db.Categories.Add(category);
        db.Recipes.Add(recipe);

        // Add slug history record for old slug
        db.RecipeSlugHistories.Add(new RecipeSlugHistory
        {
            Id = Guid.NewGuid(),
            RecipeId = recipeId,
            Slug = "pho-bo-cu"
        });

        await db.SaveChangesAsync();

        var handler = new GetRecipeBySlugHandler(db);
        var query = new GetRecipeBySlugQuery("pho-bo-cu"); // Query via old slug

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(recipeId);
        result.Slug.Should().Be("pho-bo-ha-noi-dac-biet");
    }
}
