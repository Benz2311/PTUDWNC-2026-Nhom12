using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using CulinaryBlog.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application;

/// <summary>
/// Unit tests cho Recipe Query Handlers: GetRecipes, GetRecipeBySlug, GetRecipesByCategory.
/// Kiểm tra Filter, Sort, Pagination và xử lý slug hợp lệ/không tồn tại.
/// </summary>
public class RecipeQueryHandlerTests
{
    private readonly Mock<IRecipeRepository> _recipeRepoMock;

    public RecipeQueryHandlerTests()
    {
        _recipeRepoMock = new Mock<IRecipeRepository>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Dữ liệu mẫu dùng chung
    // ─────────────────────────────────────────────────────────────────────────

    private static RecipeCategoryDto MakeCategory(string name = "Món Chính", string slug = "mon-chinh")
        => new(Guid.NewGuid(), name, slug, null);

    private static RecipeListItemDto MakeListItem(string title, string slug, DateTime? publishedAt = null)
        => new(
            Guid.NewGuid(), title, slug,
            "Mô tả ngắn", 15, 30, 2, "Easy",
            null,
            MakeCategory(),
            publishedAt ?? DateTime.UtcNow.AddDays(-1));

    private static PagedResultDto<RecipeListItemDto> MakePaged(
        IReadOnlyList<RecipeListItemDto> items, int total = -1, int page = 1, int pageSize = 12)
    {
        var count = total < 0 ? items.Count : total;
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);
        return new PagedResultDto<RecipeListItemDto>(items, count, page, pageSize, totalPages);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetRecipesQueryHandler — danh sách + phân trang
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecipes_ReturnsPagedResult_FromRepository()
    {
        // Arrange
        var items = new List<RecipeListItemDto>
        {
            MakeListItem("Phở Bò",  "pho-bo"),
            MakeListItem("Bún Chả", "bun-cha"),
        };
        var paged = MakePaged(items, total: 2);

        _recipeRepoMock
            .Setup(r => r.GetPublishedAsync(1, 12, It.IsAny<RecipeListOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var handler = new GetRecipesQueryHandler(_recipeRepoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetRecipesQuery(Page: 1, PageSize: 12, Options: null),
            CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetRecipes_WithSearchOption_PassesOptionsToRepository()
    {
        // Arrange
        var options = new RecipeListOptions(Search: "phở");
        var paged = MakePaged(new List<RecipeListItemDto> { MakeListItem("Phở Bò", "pho-bo") });

        RecipeListOptions? capturedOptions = null;
        _recipeRepoMock
            .Setup(r => r.GetPublishedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<RecipeListOptions>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, RecipeListOptions, CancellationToken>(
                (_, _, opts, _) => capturedOptions = opts)
            .ReturnsAsync(paged);

        var handler = new GetRecipesQueryHandler(_recipeRepoMock.Object);

        // Act
        await handler.Handle(new GetRecipesQuery(1, 12, options), CancellationToken.None);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Search.Should().Be("phở");
    }

    [Fact]
    public async Task GetRecipes_WhenNoOptions_UsesDefaultOptions()
    {
        // Arrange
        var paged = MakePaged(new List<RecipeListItemDto>());

        RecipeListOptions? capturedOptions = null;
        _recipeRepoMock
            .Setup(r => r.GetPublishedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<RecipeListOptions>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, RecipeListOptions, CancellationToken>(
                (_, _, opts, _) => capturedOptions = opts)
            .ReturnsAsync(paged);

        var handler = new GetRecipesQueryHandler(_recipeRepoMock.Object);

        // Act
        await handler.Handle(new GetRecipesQuery(1, 12, Options: null), CancellationToken.None);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Search.Should().BeNull();
        capturedOptions.CategorySlug.Should().BeNull();
        capturedOptions.SortBy.Should().Be(RecipeSortField.PublishedAt);
        capturedOptions.SortDescending.Should().BeTrue();
    }

    [Theory]
    [InlineData(RecipeSortField.Title,     false)]
    [InlineData(RecipeSortField.PrepTime,  true)]
    [InlineData(RecipeSortField.CookTime,  false)]
    [InlineData(RecipeSortField.TotalTime, true)]
    public async Task GetRecipes_DifferentSortOptions_PassedCorrectlyToRepository(
        RecipeSortField sortBy, bool descending)
    {
        // Arrange
        var options = new RecipeListOptions(SortBy: sortBy, SortDescending: descending);
        var paged = MakePaged(new List<RecipeListItemDto>());

        RecipeListOptions? captured = null;
        _recipeRepoMock
            .Setup(r => r.GetPublishedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<RecipeListOptions>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, RecipeListOptions, CancellationToken>(
                (_, _, opts, _) => captured = opts)
            .ReturnsAsync(paged);

        var handler = new GetRecipesQueryHandler(_recipeRepoMock.Object);

        // Act
        await handler.Handle(new GetRecipesQuery(1, 12, options), CancellationToken.None);

        // Assert
        captured!.SortBy.Should().Be(sortBy);
        captured.SortDescending.Should().Be(descending);
    }

    [Fact]
    public async Task GetRecipes_Pagination_TotalPagesCalculatedCorrectly()
    {
        // Arrange — 25 bản ghi, pageSize=12 => 3 trang
        var paged = MakePaged(
            new List<RecipeListItemDto>(), total: 25, page: 1, pageSize: 12);

        _recipeRepoMock
            .Setup(r => r.GetPublishedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<RecipeListOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var handler = new GetRecipesQueryHandler(_recipeRepoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetRecipesQuery(1, 12, null), CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(25);
        result.Total.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetRecipeBySlugQueryHandler — chi tiết recipe
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecipeBySlug_WithValidSlug_ReturnsRecipeDetail()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var detail = new RecipeDetailDto(
            Guid.NewGuid(), "Phở Bò Hà Nội", "pho-bo-ha-noi",
            "Phở truyền thống", "Hướng dẫn chi tiết...",
            30, 60, 4, "Medium",
            DateTime.UtcNow.AddDays(-10),
            MakeCategory("Bún, Mì & Phở", "bun-mi-pho"),
            Ingredients: new List<RecipeIngredientDto>
            {
                new(Guid.NewGuid(), "Xương bò", 500, "gram", null, 1),
                new(Guid.NewGuid(), "Bánh phở", 400, "gram", null, 2),
            },
            Steps: new List<RecipeStepDto>
            {
                new(Guid.NewGuid(), 1, "Ninh xương", "Ninh 4-6 tiếng."),
                new(Guid.NewGuid(), 2, "Trụng bánh", "Trụng bánh phở nhanh."),
            },
            Images: new List<RecipeImageDto>
            {
                new(Guid.NewGuid(), "https://example.com/pho-bo.jpg", "Phở bò", true, 1),
            },
            Nutrition: new RecipeNutritionDto(350, 25, 40, 8, 2, 600),
            Author: new RecipeAuthorDto(authorId, "Nguyễn Văn Bếp", "https://example.com/avatar.jpg"));

        _recipeRepoMock
            .Setup(r => r.GetPublishedBySlugAsync("pho-bo-ha-noi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var handler = new GetRecipeBySlugQueryHandler(_recipeRepoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetRecipeBySlugQuery("pho-bo-ha-noi"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Phở Bò Hà Nội");
        result.Slug.Should().Be("pho-bo-ha-noi");
        result.Ingredients.Should().HaveCount(2);
        result.Steps.Should().HaveCount(2);
        result.Images.Should().HaveCount(1);
        result.Nutrition.Calories.Should().Be(350);
        result.Author.Should().NotBeNull();
        result.Author!.DisplayName.Should().Be("Nguyễn Văn Bếp");
        result.Author.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public async Task GetRecipeBySlug_WithNonExistentSlug_ReturnsNull()
    {
        // Arrange
        _recipeRepoMock
            .Setup(r => r.GetPublishedBySlugAsync(
                "slug-khong-ton-tai", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecipeDetailDto?)null);

        var handler = new GetRecipeBySlugQueryHandler(_recipeRepoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetRecipeBySlugQuery("slug-khong-ton-tai"), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetRecipesByCategoryQueryHandler — recipe theo category
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecipesByCategory_WithValidSlug_ReturnsCategoryWithRecipes()
    {
        // Arrange
        var categoryDto = MakeCategory("Món Chính", "mon-chinh");
        var items = new List<RecipeListItemDto>
        {
            MakeListItem("Cơm Tấm Sườn", "com-tam-suon"),
            MakeListItem("Cá Kho Tộ",    "ca-kho-to"),
        };
        var paged = MakePaged(items, total: 2);
        var response = new CategoryRecipesResponseDto(categoryDto, paged);

        _recipeRepoMock
            .Setup(r => r.GetPublishedByCategorySlugAsync(
                "mon-chinh", 1, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var handler = new GetRecipesByCategoryQueryHandler(_recipeRepoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetRecipesByCategoryQuery("mon-chinh", 1, 12), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Category.Name.Should().Be("Món Chính");
        result.Category.Slug.Should().Be("mon-chinh");
        result.Recipes.Items.Should().HaveCount(2);
        result.Recipes.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetRecipesByCategory_WithInvalidSlug_ReturnsNull()
    {
        // Arrange
        _recipeRepoMock
            .Setup(r => r.GetPublishedByCategorySlugAsync(
                "category-khong-ton-tai", It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryRecipesResponseDto?)null);

        var handler = new GetRecipesByCategoryQueryHandler(_recipeRepoMock.Object);

        // Act
        var result = await handler.Handle(
            new GetRecipesByCategoryQuery("category-khong-ton-tai", 1, 12),
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecipesByCategory_WithPagination_PassesCorrectPageParams()
    {
        // Arrange
        var response = new CategoryRecipesResponseDto(
            MakeCategory(),
            MakePaged(new List<RecipeListItemDto>(), total: 30, page: 2, pageSize: 6));

        int capturedPage = 0, capturedPageSize = 0;
        _recipeRepoMock
            .Setup(r => r.GetPublishedByCategorySlugAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, int, int, CancellationToken>(
                (_, p, ps, _) => { capturedPage = p; capturedPageSize = ps; })
            .ReturnsAsync(response);

        var handler = new GetRecipesByCategoryQueryHandler(_recipeRepoMock.Object);

        // Act
        await handler.Handle(
            new GetRecipesByCategoryQuery("mon-chinh", Page: 2, PageSize: 6),
            CancellationToken.None);

        // Assert
        capturedPage.Should().Be(2);
        capturedPageSize.Should().Be(6);
    }

    [Fact]
    public async Task GetRecipes_WithCategoryIdAndMaxCookTime_PassesCorrectOptions()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var options = new RecipeListOptions(
            CategoryId: categoryId,
            MaxCookTimeMinutes: 45);

        RecipeListOptions? captured = null;
        _recipeRepoMock
            .Setup(r => r.GetPublishedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<RecipeListOptions>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, RecipeListOptions, CancellationToken>(
                (_, _, opts, _) => captured = opts)
            .ReturnsAsync(MakePaged(new List<RecipeListItemDto>()));

        var handler = new GetRecipesQueryHandler(_recipeRepoMock.Object);

        // Act
        await handler.Handle(new GetRecipesQuery(1, 12, options), CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.CategoryId.Should().Be(categoryId);
        captured.MaxCookTimeMinutes.Should().Be(45);
    }
}
