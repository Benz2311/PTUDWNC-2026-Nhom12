using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Categories;

public class GetCategoriesQueryHandlerTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock;
    private readonly GetCategoriesQueryHandler _handler;

    public GetCategoriesQueryHandlerTests()
    {
        _repositoryMock = new Mock<ICategoryRepository>();
        _handler = new GetCategoriesQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsCategories_ReturnsMappedDtos()
    {
        // Arrange
        var catA = Category.Create("Món Chính", orderIndex: 2);
        var catB = Category.Create("Khai Vị", orderIndex: 1);

        _repositoryMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { catA, catB });

        // Act
        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Khai Vị", "categories are ordered by OrderIndex ascending");
        result[1].Name.Should().Be("Món Chính");
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryExactlyOnce()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        // Act
        await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ResultItemsAreCategoryDtoType()
    {
        // Arrange
        var category = Category.Create("Tráng Miệng");
        _repositoryMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { category });

        // Act
        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().AllBeOfType<CategoryDto>();
    }

    [Fact]
    public async Task Handle_CategoriesWithSameOrderIndex_OrderedByNameAscending()
    {
        // Arrange — use ASCII names so default string ordering is predictable
        var catB = Category.Create("Beverages", orderIndex: 0);
        var catA = Category.Create("Appetizers", orderIndex: 0);
        var catC = Category.Create("Desserts", orderIndex: 0);

        _repositoryMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { catB, catA, catC });

        // Act
        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Select(r => r.Name).Should().BeInAscendingOrder();
    }
}
