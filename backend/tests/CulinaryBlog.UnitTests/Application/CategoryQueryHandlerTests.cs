using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryStatistics;
using FluentAssertions;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application;

/// <summary>
/// Unit tests cho Category Query Handlers.
/// Kiểm tra logic xử lý của handler mà không cần database thực.
/// </summary>
public class CategoryQueryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock;

    public CategoryQueryHandlerTests()
    {
        _categoryRepoMock = new Mock<ICategoryRepository>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCategoriesQueryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategories_ReturnsAllCategoriesFromRepository()
    {
        // Arrange
        var expected = new List<CategoryDto>
        {
            new(Guid.NewGuid(), "Món Chính",       "mon-chinh",       "Các món chính", null, 1, 5),
            new(Guid.NewGuid(), "Món Tráng Miệng", "mon-trang-mieng", "Tráng miệng",   null, 2, 3),
        };

        _categoryRepoMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetCategoriesQueryHandler(_categoryRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expected);
        _categoryRepoMock.Verify(
            r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCategories_WhenNoCategoriesExist_ReturnsEmptyList()
    {
        // Arrange
        _categoryRepoMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryDto>());

        var handler = new GetCategoriesQueryHandler(_categoryRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategories_EachDto_ContainsCorrectFields()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var expected = new List<CategoryDto>
        {
            new(categoryId, "Đồ Uống", "do-uong", "Nước ép, sinh tố", "https://example.com/img.jpg", 3, 10),
        };

        _categoryRepoMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetCategoriesQueryHandler(_categoryRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        var category = result.Should().ContainSingle().Which;
        category.Id.Should().Be(categoryId);
        category.Name.Should().Be("Đồ Uống");
        category.Slug.Should().Be("do-uong");
        category.RecipeCount.Should().Be(10);
        category.OrderIndex.Should().Be(3);
        category.SortOrder.Should().Be(3);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCategoryStatisticsQueryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategoryStatistics_ReturnsStatisticsFromRepository()
    {
        // Arrange
        var statistics = new CategoryStatisticsDto(
            TotalCategories: 20,
            TotalRecipes:    100,
            PublishedRecipes: 85,
            DraftRecipes:     15,
            Categories: new[]
            {
                new CategoryStatisticItemDto(Guid.NewGuid(), "Món Chính",   30),
                new CategoryStatisticItemDto(Guid.NewGuid(), "Đồ Uống",     15),
                new CategoryStatisticItemDto(Guid.NewGuid(), "Ăn Vặt",      12),
            });

        _categoryRepoMock
            .Setup(r => r.GetCategoryStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);

        var handler = new GetCategoryStatisticsQueryHandler(_categoryRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetCategoryStatisticsQuery(), CancellationToken.None);

        // Assert
        result.TotalCategories.Should().Be(20);
        result.TotalRecipes.Should().Be(100);
        result.PublishedRecipes.Should().Be(85);
        result.DraftRecipes.Should().Be(15);
        result.Categories.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCategoryStatistics_PublishedPlusDraft_EqualsTotalRecipes()
    {
        // Arrange
        var statistics = new CategoryStatisticsDto(
            TotalCategories:  5,
            TotalRecipes:    60,
            PublishedRecipes: 45,
            DraftRecipes:     15,
            Categories: Array.Empty<CategoryStatisticItemDto>());

        _categoryRepoMock
            .Setup(r => r.GetCategoryStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);

        var handler = new GetCategoryStatisticsQueryHandler(_categoryRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetCategoryStatisticsQuery(), CancellationToken.None);

        // Assert — published + draft = total
        (result.PublishedRecipes + result.DraftRecipes)
            .Should().Be(result.TotalRecipes);
    }

    [Fact]
    public async Task GetCategoryStatistics_CallsRepositoryExactlyOnce()
    {
        // Arrange
        _categoryRepoMock
            .Setup(r => r.GetCategoryStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CategoryStatisticsDto(0, 0, 0, 0, Array.Empty<CategoryStatisticItemDto>()));

        var handler = new GetCategoryStatisticsQueryHandler(_categoryRepoMock.Object);

        // Act
        await handler.Handle(new GetCategoryStatisticsQuery(), CancellationToken.None);

        // Assert
        _categoryRepoMock.Verify(
            r => r.GetCategoryStatisticsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
