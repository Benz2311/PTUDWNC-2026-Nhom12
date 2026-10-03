using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Application.Features.Categories.Commands.CreateCategory;
using CulinaryBlog.Application.Features.Categories.Commands.UpdateCategory;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application;

public class CategoryCommandHandlerTests
{
    [Fact]
    public async Task CreateCategory_WhenSlugExists_AddsSuffixAndInvalidatesListCache()
    {
        var categoryRepository = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<CulinaryBlog.Application.Common.Interfaces.ICacheService>();
        Category? savedCategory = null;

        categoryRepository
            .Setup(repository => repository.NameExistsAsync(
                "Món Chính",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        categoryRepository
            .Setup(repository => repository.SlugExistsAsync(
                "mon-chinh",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        categoryRepository
            .Setup(repository => repository.SlugExistsAsync(
                "mon-chinh-2",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        categoryRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<Category>(),
                It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((category, _) => savedCategory = category)
            .Returns(Task.CompletedTask);

        var handler = new CreateCategoryCommandHandler(
            categoryRepository.Object,
            unitOfWork.Object,
            cache.Object);

        var result = await handler.Handle(
            new CreateCategoryCommand(" Món Chính ", "Mô tả"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("mon-chinh-2");
        result.Name.Should().Be("Món Chính");
        savedCategory.Should().NotBeNull();
        unitOfWork.Verify(
            item => item.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        cache.Verify(
            item => item.RemoveAsync("categories:all", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateCategory_WhenNameExists_DoesNotSaveOrInvalidateCache()
    {
        var categoryRepository = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<CulinaryBlog.Application.Common.Interfaces.ICacheService>();
        categoryRepository
            .Setup(repository => repository.NameExistsAsync(
                "Món Chính",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateCategoryCommandHandler(
            categoryRepository.Object,
            unitOfWork.Object,
            cache.Object);

        var result = await handler.Handle(
            new CreateCategoryCommand("Món Chính"),
            CancellationToken.None);

        result.Should().BeNull();
        unitOfWork.Verify(
            item => item.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cache.Verify(
            item => item.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void CreateCategoryValidator_WhenNameIsBlank_ReturnsValidationError()
    {
        var validator = new CreateCategoryCommandValidator();

        var result = validator.Validate(new CreateCategoryCommand("   "));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Name");
    }

    [Fact]
    public async Task UpdateCategory_ChangesNameAndDescriptionWithoutChangingSlug()
    {
        var category = Category.Create("Old name", "stable-slug", "Old description");
        var categoryRepository = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<CulinaryBlog.Application.Common.Interfaces.ICacheService>();
        categoryRepository
            .Setup(repository => repository.GetByIdAsync(
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        categoryRepository
            .Setup(repository => repository.NameExistsAsync(
                "New name",
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        unitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        cache
            .Setup(item => item.RemoveAsync(
                "categories:all",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateCategoryCommandHandler(
            categoryRepository.Object,
            unitOfWork.Object,
            cache.Object);

        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, " New name ", null),
            CancellationToken.None);

        result.Should().Be(UpdateCategoryResult.Updated);
        category.Name.Should().Be("New name");
        category.Description.Should().BeNull();
        category.Slug.Should().Be("stable-slug");
        categoryRepository.Verify(repository => repository.Update(category), Times.Once);
        unitOfWork.Verify(
            item => item.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        cache.Verify(
            item => item.RemoveAsync("categories:all", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCategory_WhenNameExists_DoesNotSaveOrInvalidateCache()
    {
        var category = Category.Create("Old name", "stable-slug");
        var categoryRepository = new Mock<ICategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new Mock<CulinaryBlog.Application.Common.Interfaces.ICacheService>();
        categoryRepository
            .Setup(repository => repository.GetByIdAsync(
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        categoryRepository
            .Setup(repository => repository.NameExistsAsync(
                "Taken name",
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UpdateCategoryCommandHandler(
            categoryRepository.Object,
            unitOfWork.Object,
            cache.Object);

        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, "Taken name", "Description"),
            CancellationToken.None);

        result.Should().Be(UpdateCategoryResult.NameAlreadyExists);
        unitOfWork.Verify(
            item => item.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cache.Verify(
            item => item.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}