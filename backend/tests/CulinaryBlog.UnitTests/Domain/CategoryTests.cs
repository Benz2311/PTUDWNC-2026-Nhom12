using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.UnitTests.Domain;

public class CategoryTests
{
    // ── Category.Create ───────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidName_ReturnsCategoryWithGeneratedSlug()
    {
        // Act
        var category = Category.Create("Món Khai Vị");

        // Assert
        category.Should().NotBeNull();
        category.Name.Should().Be("Món Khai Vị");
        category.Slug.Should().NotBeNullOrEmpty();
        category.Id.Should().NotBeEmpty();
        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_WithExplicitSlug_UsesThatSlug()
    {
        // Arrange
        const string customSlug = "mon-khai-vi-custom";

        // Act
        var category = Category.Create("Món Khai Vị", slug: customSlug);

        // Assert
        category.Slug.Should().Be(customSlug);
    }

    [Fact]
    public void Create_WithDescription_SetsDescription()
    {
        // Act
        var category = Category.Create("Tráng Miệng", description: "Các món ngọt sau bữa ăn");

        // Assert
        category.Description.Should().Be("Các món ngọt sau bữa ăn");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Act
        var act = () => Category.Create(invalidName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithOrderIndex_SetsOrderIndex()
    {
        // Act
        var category = Category.Create("Cơm", orderIndex: 5);

        // Assert
        category.OrderIndex.Should().Be(5);
    }

    // ── Category.Update ───────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var category = Category.Create("Old Name");

        // Act
        category.Update("New Name", description: "Updated desc", orderIndex: 2);

        // Assert
        category.Name.Should().Be("New Name");
        category.Description.Should().Be("Updated desc");
        category.OrderIndex.Should().Be(2);
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithNewSlug_UpdatesSlug()
    {
        // Arrange
        var category = Category.Create("Tên Cũ");

        // Act
        category.Update("Tên Mới", slug: "ten-moi-custom");

        // Assert
        category.Slug.Should().Be("ten-moi-custom");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var category = Category.Create("Tên Hợp Lệ");

        // Act
        var act = () => category.Update(invalidName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // ── Category.SoftDelete ───────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsIsDeletedTrue_AndUpdatesTimestamp()
    {
        // Arrange
        var category = Category.Create("Danh Mục");

        // Act
        category.SoftDelete();

        // Assert
        category.IsDeleted.Should().BeTrue();
        category.UpdatedAt.Should().NotBeNull();
    }

    // ── Category.AddRecipe ────────────────────────────────────────────────────

    [Fact]
    public void AddRecipe_WithValidRecipe_AddsToCollection()
    {
        // Arrange
        var category = Category.Create("Cơm");
        var recipe = new Recipe(
            title: "Cơm Tấm",
            slug: "com-tam",
            description: null,
            prepTimeMinutes: 15,
            cookTimeMinutes: 30,
            servings: 2,
            difficulty: DifficultyLevel.Easy,
            status: RecipeStatus.Draft,
            categoryId: category.Id);

        // Act
        category.AddRecipe(recipe);

        // Assert
        category.Recipes.Should().ContainSingle();
    }

    [Fact]
    public void AddRecipe_SameRecipeTwice_DoesNotDuplicate()
    {
        // Arrange
        var category = Category.Create("Cơm");
        var recipe = new Recipe(
            "Cơm Tấm", "com-tam", null, 10, 20, 1,
            DifficultyLevel.Easy, RecipeStatus.Draft, category.Id);

        // Act
        category.AddRecipe(recipe);
        category.AddRecipe(recipe);

        // Assert
        category.Recipes.Should().ContainSingle("duplicate recipe should not be added twice");
    }

    [Fact]
    public void AddRecipe_NullRecipe_ThrowsArgumentNullException()
    {
        // Arrange
        var category = Category.Create("Cơm");

        // Act
        var act = () => category.AddRecipe(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
