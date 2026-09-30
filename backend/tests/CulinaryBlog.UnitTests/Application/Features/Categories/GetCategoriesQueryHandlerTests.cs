using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
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
    public async Task Handle_WhenRepositoryReturnsDtos_ReturnsThemInRepositoryOrder()
    {
        // Arrange
        var categories = new List<CategoryDto>
        {
            new(Guid.NewGuid(), "Appetizers", "appetizers", null, null, 1, 3),
            new(Guid.NewGuid(), "Main dishes", "main-dishes", null, null, 2, 5)
        };

        _repositoryMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(categories);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryDto>());

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
            .ReturnsAsync(new List<CategoryDto>());

        // Act
        await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsProjectedCategoryFields()
    {
        // Arrange
        var category = new CategoryDto(
            Guid.NewGuid(),
            "Desserts",
            "desserts",
            "Sweet dishes",
            null,
            3,
            2);
        _repositoryMock
            .Setup(r => r.GetAllWithRecipeCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryDto> { category });

        // Act
        var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(category);
    }
}
