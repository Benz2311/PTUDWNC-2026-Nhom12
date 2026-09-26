using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipeBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipes;
using CulinaryBlog.Application.Features.Recipes.Queries.GetRecipesByCategory;
using FluentAssertions;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Recipes;

public class RecipeQueryHandlerTests
{
    [Fact]
    public async Task GetRecipesHandler_DelegatesPaginationToRepository()
    {
        var repository = new Mock<IRecipeRepository>();
        var expected = new PagedResultDto<RecipeListItemDto>(
            [],
            0,
            2,
            8,
            0);
        var cancellationToken = new CancellationTokenSource().Token;
        repository
            .Setup(item => item.GetPublishedAsync(2, 8, cancellationToken))
            .ReturnsAsync(expected);
        var handler = new GetRecipesQueryHandler(repository.Object);

        var result = await handler.Handle(
            new GetRecipesQuery(2, 8),
            cancellationToken);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetRecipeBySlugHandler_DelegatesSlugToRepository()
    {
        var repository = new Mock<IRecipeRepository>();
        var expected = new RecipeDetailDto(
            Guid.NewGuid(),
            "Sour soup",
            "sour-soup",
            "Description",
            "Content",
            15,
            20,
            4,
            "Easy",
            DateTime.UtcNow,
            new RecipeCategoryDto(Guid.NewGuid(), "Main dishes", "main-dishes", null),
            [],
            [],
            [],
            new RecipeNutritionDto(null, null, null, null, null, null));
        var cancellationToken = new CancellationTokenSource().Token;
        repository
            .Setup(item => item.GetPublishedBySlugAsync("sour-soup", cancellationToken))
            .ReturnsAsync(expected);
        var handler = new GetRecipeBySlugQueryHandler(repository.Object);

        var result = await handler.Handle(
            new GetRecipeBySlugQuery("sour-soup"),
            cancellationToken);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetRecipesByCategoryHandler_DelegatesCategoryAndPagination()
    {
        var repository = new Mock<IRecipeRepository>();
        var expected = new CategoryRecipesResponseDto(
            new RecipeCategoryDto(Guid.NewGuid(), "Main dishes", "main-dishes", null),
            new PagedResultDto<RecipeListItemDto>([], 0, 1, 12, 0));
        var cancellationToken = new CancellationTokenSource().Token;
        repository
            .Setup(item => item.GetPublishedByCategorySlugAsync(
                "main-dishes",
                1,
                12,
                cancellationToken))
            .ReturnsAsync(expected);
        var handler = new GetRecipesByCategoryQueryHandler(repository.Object);

        var result = await handler.Handle(
            new GetRecipesByCategoryQuery("main-dishes"),
            cancellationToken);

        result.Should().BeSameAs(expected);
    }
}