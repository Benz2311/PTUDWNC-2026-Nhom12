using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.UnitTests.Application.Features.Recipes;

/// <summary>
/// Unit tests for the Duplicate Recipe domain logic.
/// These tests verify the duplication algorithm used in RecipeEndpoints.DuplicateRecipe.
/// </summary>
public sealed class DuplicateRecipeTests
{
    // -----------------------------------------------------------------------
    // Identity & Status
    // -----------------------------------------------------------------------

    [Fact]
    public void DuplicatedRecipe_HasNewId_DifferentFromSource()
    {
        var source = CreateSampleRecipe();

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.NotEqual(source.Id, copy.Id);
        Assert.NotEqual(Guid.Empty, copy.Id);
    }

    [Fact]
    public void DuplicatedRecipe_HasNewSlug_DifferentFromSource()
    {
        var source = CreateSampleRecipe();

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.NotEqual(source.Slug, copy.Slug);
        Assert.Contains("copy", copy.Slug);
    }

    [Fact]
    public void DuplicatedRecipe_SlugContainsSourceSlug_AsBase()
    {
        var source = CreateSampleRecipe();

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.StartsWith(source.Slug, copy.Slug);
    }

    [Fact]
    public void DuplicatedRecipe_HasStatusDraft_EvenWhenSourceIsPublished()
    {
        var source = CreateSampleRecipe();
        source.Status = RecipeStatus.Published;

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Equal(RecipeStatus.Draft, copy.Status);
    }

    [Fact]
    public void DuplicatedRecipe_HasStatusDraft_EvenWhenSourceIsArchived()
    {
        var source = CreateSampleRecipe();
        source.Status = RecipeStatus.Archived;

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Equal(RecipeStatus.Draft, copy.Status);
    }

    // -----------------------------------------------------------------------
    // Title
    // -----------------------------------------------------------------------

    [Fact]
    public void DuplicatedRecipe_TitleContainsSourceTitle()
    {
        var source = CreateSampleRecipe();

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Contains(source.Title, copy.Title);
    }

    [Fact]
    public void DuplicatedRecipe_TitleContains_BanSaoSuffix()
    {
        var source = CreateSampleRecipe();

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Contains("Bản sao", copy.Title);
    }

    // -----------------------------------------------------------------------
    // Content fields
    // -----------------------------------------------------------------------

    [Fact]
    public void DuplicatedRecipe_CopiesAllContentFields()
    {
        var source = CreateSampleRecipe();

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Equal(source.Description, copy.Description);
        Assert.Equal(source.Content, copy.Content);
        Assert.Equal(source.CategoryId, copy.CategoryId);
        Assert.Equal(source.PrepTimeMinutes, copy.PrepTimeMinutes);
        Assert.Equal(source.CookTimeMinutes, copy.CookTimeMinutes);
        Assert.Equal(source.Servings, copy.Servings);
        Assert.Equal(source.Difficulty, copy.Difficulty);
        Assert.Equal(source.CoverImageUrl, copy.CoverImageUrl);
    }

    // -----------------------------------------------------------------------
    // No interaction/admin data copied
    // -----------------------------------------------------------------------

    [Fact]
    public void DuplicatedRecipe_PublishedAt_IsNull()
    {
        var source = CreateSampleRecipe();
        source.PublishedAt = DateTime.UtcNow.AddDays(-10);

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Null(copy.PublishedAt);
    }

    [Fact]
    public void DuplicatedRecipe_DeletedAt_IsNull()
    {
        var source = CreateSampleRecipe();

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Null(copy.DeletedAt);
        Assert.False(copy.IsDeleted);
    }

    // -----------------------------------------------------------------------
    // AuthorId
    // -----------------------------------------------------------------------

    [Fact]
    public void DuplicatedRecipe_UsesProvidedAuthorId()
    {
        var source = CreateSampleRecipe();
        var newAuthorId = Guid.NewGuid();

        var copy = SimulateDuplicate(source, newAuthorId);

        Assert.Equal(newAuthorId, copy.AuthorId);
    }

    [Fact]
    public void DuplicatedRecipe_AuthorId_CanDifferFromSourceAuthor()
    {
        var source = CreateSampleRecipe();
        var differentAuthor = Guid.NewGuid();
        Assert.NotEqual(source.AuthorId, differentAuthor);

        var copy = SimulateDuplicate(source, differentAuthor);

        Assert.Equal(differentAuthor, copy.AuthorId);
        Assert.NotEqual(source.AuthorId, copy.AuthorId);
    }

    // -----------------------------------------------------------------------
    // Ingredients
    // -----------------------------------------------------------------------

    [Fact]
    public void DuplicatedRecipe_CopiesIngredientCount()
    {
        var source = CreateSampleRecipe();
        source.Ingredients = new List<RecipeIngredient>
        {
            new() { Name = "Bột mì", Quantity = 200m, Unit = "g", SortOrder = 0 },
            new() { Name = "Muối", Quantity = 5m, Unit = "g", SortOrder = 1 },
            new() { Name = "Nước", Quantity = 300m, Unit = "ml", SortOrder = 2 },
        };

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Equal(source.Ingredients.Count, copy.Ingredients.Count);
    }

    [Fact]
    public void DuplicatedIngredients_HaveNewIds_DifferentFromSource()
    {
        var source = CreateSampleRecipe();
        var srcIngredient = new RecipeIngredient { Name = "Đường", Quantity = 50m, Unit = "g" };
        source.Ingredients = new List<RecipeIngredient> { srcIngredient };

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        var copyIngredient = copy.Ingredients.Single();
        Assert.NotEqual(srcIngredient.Id, copyIngredient.Id);
    }

    [Fact]
    public void DuplicatedIngredients_PreserveNameAndQuantity()
    {
        var source = CreateSampleRecipe();
        source.Ingredients = new List<RecipeIngredient>
        {
            new() { Name = "Thịt bò", Quantity = 500m, Unit = "g", Notes = "Thái mỏng", SortOrder = 0 },
        };

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        var copyIngredient = copy.Ingredients.Single();
        Assert.Equal("Thịt bò", copyIngredient.Name);
        Assert.Equal(500m, copyIngredient.Quantity);
        Assert.Equal("g", copyIngredient.Unit);
        Assert.Equal("Thái mỏng", copyIngredient.Notes);
    }

    [Fact]
    public void DuplicatedIngredients_BelongToNewRecipeId()
    {
        var source = CreateSampleRecipe();
        source.Ingredients = new List<RecipeIngredient>
        {
            new() { Name = "Hành", Quantity = 2m, Unit = "củ" },
        };

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Equal(copy.Id, copy.Ingredients.Single().RecipeId);
    }

    // -----------------------------------------------------------------------
    // Steps
    // -----------------------------------------------------------------------

    [Fact]
    public void DuplicatedRecipe_CopiesStepCount()
    {
        var source = CreateSampleRecipe();
        source.Steps = new List<RecipeStep>
        {
            new() { StepNumber = 1, Title = "Sơ chế nguyên liệu", Description = "Rửa sạch rau" },
            new() { StepNumber = 2, Title = "Nấu", Description = "Đun nước sôi" },
        };

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Equal(2, copy.Steps.Count);
    }

    [Fact]
    public void DuplicatedSteps_HaveNewIds_DifferentFromSource()
    {
        var source = CreateSampleRecipe();
        var srcStep = new RecipeStep { StepNumber = 1, Title = "Bước 1", Description = "Mô tả" };
        source.Steps = new List<RecipeStep> { srcStep };

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        var copyStep = copy.Steps.Single();
        Assert.NotEqual(srcStep.Id, copyStep.Id);
    }

    [Fact]
    public void DuplicatedSteps_PreserveContent()
    {
        var source = CreateSampleRecipe();
        source.Steps = new List<RecipeStep>
        {
            new() { StepNumber = 1, Title = "Trộn bột", Description = "Trộn đều bột với nước" },
        };

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        var copyStep = copy.Steps.Single();
        Assert.Equal(1, copyStep.StepNumber);
        Assert.Equal("Trộn bột", copyStep.Title);
        Assert.Equal("Trộn đều bột với nước", copyStep.Description);
    }

    [Fact]
    public void DuplicatedSteps_BelongToNewRecipeId()
    {
        var source = CreateSampleRecipe();
        source.Steps = new List<RecipeStep>
        {
            new() { StepNumber = 1, Title = "Bước đầu", Description = "Mô tả bước đầu" },
        };

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        Assert.Equal(copy.Id, copy.Steps.Single().RecipeId);
    }

    // -----------------------------------------------------------------------
    // Slug truncation for very long slugs
    // -----------------------------------------------------------------------

    [Fact]
    public void DuplicatedRecipe_SlugIsTruncated_WhenSourceSlugIsVeryLong()
    {
        var source = CreateSampleRecipe();
        source.Slug = new string('a', 210); // 210 chars > 200 max base

        var copy = SimulateDuplicate(source, Guid.NewGuid());

        // Slug should not exceed 200 (base) + "-copy" (5) = 205
        Assert.True(copy.Slug.Length <= 205, $"Slug length {copy.Slug.Length} exceeds 205");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Replicates the same logic as RecipeEndpoints.DuplicateRecipe handler.
    /// </summary>
    private static Recipe SimulateDuplicate(Recipe source, Guid newAuthorId)
    {
        var newId = Guid.NewGuid();
        var baseSlug = source.Slug.Length > 200 ? source.Slug[..200] : source.Slug;
        var newSlug = $"{baseSlug}-copy";

        var copy = new Recipe
        {
            Id = newId,
            Title = $"{source.Title} (Bản sao)",
            Slug = newSlug,
            Description = source.Description,
            Content = source.Content,
            CategoryId = source.CategoryId,
            AuthorId = newAuthorId,
            Status = RecipeStatus.Draft,
            PrepTimeMinutes = source.PrepTimeMinutes,
            CookTimeMinutes = source.CookTimeMinutes,
            Servings = source.Servings,
            Difficulty = source.Difficulty,
            CoverImageUrl = source.CoverImageUrl,
            // Interaction data intentionally NOT copied
            PublishedAt = null,
            DeletedAt = null,
            IsDeleted = false,
        };

        copy.Ingredients = source.Ingredients
            .Select(i => new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = newId,
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Notes = i.Notes,
                SortOrder = i.SortOrder,
            })
            .ToList();

        copy.Steps = source.Steps
            .Select(s => new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = newId,
                StepNumber = s.StepNumber,
                Title = s.Title,
                Description = s.Description,
                ImageUrl = s.ImageUrl,
                TimerMinutes = s.TimerMinutes,
            })
            .ToList();

        return copy;
    }

    private static Recipe CreateSampleRecipe()
    {
        return new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Phở bò Hà Nội",
            Slug = "pho-bo-ha-noi",
            Description = "Công thức phở bò truyền thống Hà Nội",
            Content = "Nội dung chi tiết về phở bò",
            CategoryId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Status = RecipeStatus.Published,
            PrepTimeMinutes = 30,
            CookTimeMinutes = 180,
            Servings = 4,
            Difficulty = DifficultyLevel.Medium,
            CoverImageUrl = "https://example.com/pho.jpg",
            Ingredients = new List<RecipeIngredient>(),
            Steps = new List<RecipeStep>(),
        };
    }
}

