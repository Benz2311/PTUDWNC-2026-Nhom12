using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Recipes;

public class RecipeImageCommandHandlerTests
{
    private static readonly byte[] ValidJpegBytes = CreateValidJpeg();

    private static byte[] CreateValidJpeg()
    {
        using var img = new Image<Rgba32>(1, 1);
        using var ms = new MemoryStream();
        img.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task AddImage_FirstImage_AutomaticallyBecomesPrimary()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "first-img-recipe", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        var command = new AddRecipeImageCommand(
            RecipeId: recipeId,
            OriginalUrl: "https://minio.local/img1.jpg",
            AltText: "First Image",
            IsPrimary: false, // Even if requested false, first image must be primary!
            CurrentUserId: authorId
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsPrimary.Should().BeTrue();
        result.AltText.Should().Be("First Image");

        var dbImg = db.RecipeImages.FirstOrDefault(img => img.RecipeId == recipeId);
        dbImg.Should().NotBeNull();
        dbImg!.IsPrimary.Should().BeTrue();
        cacheMock.Verify(c => c.RemoveAsync("recipe:first-img-recipe", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddImage_SecondImageAsPrimary_DemotesFirstImagePrimaryFlag()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var img1Id = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "primary-switch-recipe", IsDeleted = false };
        db.Recipes.Add(recipe);
        db.RecipeImages.Add(new RecipeImage
        {
            Id = img1Id,
            RecipeId = recipeId,
            OriginalUrl = "https://minio.local/img1.jpg",
            IsPrimary = true,
            OrderIndex = 0,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        var command = new AddRecipeImageCommand(
            RecipeId: recipeId,
            OriginalUrl: "https://minio.local/img2.jpg",
            IsPrimary: true,
            CurrentUserId: authorId
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsPrimary.Should().BeTrue();
        result.OrderIndex.Should().Be(1);

        var dbImg1 = db.RecipeImages.Find(img1Id);
        dbImg1!.IsPrimary.Should().BeFalse(); // Demoted!
    }

    [Theory]
    [InlineData("Over 200 chars ...", 0, "AltText cannot exceed 200")]
    [InlineData("Valid", -1, "OrderIndex must be greater than or equal to 0")]
    public async Task AddImage_WhenFieldsInvalid_ThrowsValidationException(string alt, int order, string expectedError)
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "invalid-test", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);

        if (alt.StartsWith("Over 200"))
        {
            alt = new string('X', 205);
        }

        var command = new AddRecipeImageCommand(recipeId, "https://url.com/a.jpg", AltText: alt, OrderIndex: order, CurrentUserId: authorId);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Values.SelectMany(v => v).Should().Contain(msg => msg.Contains(expectedError));
    }

    [Fact]
    public async Task AddImage_WhenNotAuthorAndNotAdmin_ThrowsForbiddenException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipe = new Recipe { Id = Guid.NewGuid(), AuthorId = authorId, Slug = "img-forbidden", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        var command = new AddRecipeImageCommand(recipe.Id, "https://url.com/a.jpg", CurrentUserId: Guid.NewGuid(), IsAdmin: false);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UploadImage_WhenValid_UploadsToStorageAndSavesEntity()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var storageMock = new Mock<IStorageService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "upload-img-test", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        storageMock.Setup(s => s.UploadAsync(
            "culinaryblog",
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            "image/jpeg",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://minio:9000/culinaryblog/recipes/pic.jpg");

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        handler.SetStorageServiceForTesting(storageMock.Object);

        using var stream = new MemoryStream(ValidJpegBytes);
        var command = new UploadRecipeImageCommand(
            recipeId,
            stream,
            "dish.jpg",
            "image/jpeg",
            ValidJpegBytes.Length,
            AltText: "Delicious Dish",
            CurrentUserId: authorId
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.OriginalUrl.Should().Be("http://minio:9000/culinaryblog/recipes/pic.jpg");
        result.IsPrimary.Should().BeTrue();
        result.AltText.Should().Be("Delicious Dish");

        cacheMock.Verify(c => c.RemoveAsync("recipe:upload-img-test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadImage_WhenInvalidSignature_ThrowsValidationException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipe = new Recipe { Id = Guid.NewGuid(), AuthorId = authorId, Slug = "bad-img", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        using var badStream = new MemoryStream([0x01, 0x02, 0x03]);
        var command = new UploadRecipeImageCommand(recipe.Id, badStream, "file.jpg", "image/jpeg", 3, CurrentUserId: authorId);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateImage_WhenSettingPrimaryTrue_DemotesOtherImages()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var img1 = Guid.NewGuid();
        var img2 = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "update-img-primary", IsDeleted = false };
        db.Recipes.Add(recipe);
        db.RecipeImages.AddRange(
            new RecipeImage { Id = img1, RecipeId = recipeId, OriginalUrl = "url1", IsPrimary = true, OrderIndex = 0, IsDeleted = false },
            new RecipeImage { Id = img2, RecipeId = recipeId, OriginalUrl = "url2", IsPrimary = false, OrderIndex = 1, IsDeleted = false }
        );
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        var command = new UpdateRecipeImageCommand(recipeId, img2, AltText: "Updated Alt", IsPrimary: true, CurrentUserId: authorId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsPrimary.Should().BeTrue();
        result.AltText.Should().Be("Updated Alt");

        db.RecipeImages.Find(img1)!.IsPrimary.Should().BeFalse();
        db.RecipeImages.Find(img2)!.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateImage_WhenUnsettingPrimaryOnPrimaryImage_AutomaticallyFallsBackToSmallestOrderIndex()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var img1 = Guid.NewGuid();
        var img2 = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "fallback-img-primary", IsDeleted = false };
        db.Recipes.Add(recipe);
        db.RecipeImages.AddRange(
            new RecipeImage { Id = img1, RecipeId = recipeId, OriginalUrl = "url1", IsPrimary = true, OrderIndex = 0, IsDeleted = false },
            new RecipeImage { Id = img2, RecipeId = recipeId, OriginalUrl = "url2", IsPrimary = false, OrderIndex = 1, IsDeleted = false }
        );
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        // Unset primary on img1
        var command = new UpdateRecipeImageCommand(recipeId, img1, IsPrimary: false, CurrentUserId: authorId);

        var result = await handler.Handle(command, CancellationToken.None);

        db.RecipeImages.Find(img1)!.IsPrimary.Should().BeFalse();
        db.RecipeImages.Find(img2)!.IsPrimary.Should().BeTrue(); // Automatically promoted!
    }

    [Fact]
    public async Task SetPrimaryImage_Command_DelegatesToUpdateCommand()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var img1 = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "set-primary-recipe", IsDeleted = false };
        db.Recipes.Add(recipe);
        db.RecipeImages.Add(new RecipeImage { Id = img1, RecipeId = recipeId, OriginalUrl = "url1", IsPrimary = false, IsDeleted = false });
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        var command = new SetPrimaryRecipeImageCommand(recipeId, img1, CurrentUserId: authorId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsPrimary.Should().BeTrue();
        db.RecipeImages.Find(img1)!.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteImage_WhenPrimaryDeleted_PromotesRemainingLowestOrderIndexAndDeletesFromStorage()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var storageMock = new Mock<IStorageService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var img1 = Guid.NewGuid();
        var img2 = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "delete-img-recipe", IsDeleted = false };
        db.Recipes.Add(recipe);
        db.RecipeImages.AddRange(
            new RecipeImage
            {
                Id = img1,
                RecipeId = recipeId,
                OriginalUrl = $"recipes/{recipeId}/images/{img1}.jpg",
                IsPrimary = true,
                OrderIndex = 0,
                IsDeleted = false
            },
            new RecipeImage
            {
                Id = img2,
                RecipeId = recipeId,
                OriginalUrl = $"recipes/{recipeId}/images/{img2}.jpg",
                IsPrimary = false,
                OrderIndex = 5,
                IsDeleted = false
            }
        );
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        handler.SetStorageServiceForTesting(storageMock.Object);

        var command = new DeleteRecipeImageCommand(recipeId, img1, CurrentUserId: authorId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var dbImg1 = db.RecipeImages.Find(img1);
        dbImg1!.IsDeleted.Should().BeTrue();
        dbImg1.IsPrimary.Should().BeFalse();

        var dbImg2 = db.RecipeImages.Find(img2);
        dbImg2!.IsPrimary.Should().BeTrue(); // Promoted to primary!

        storageMock.Verify(s => s.DeleteAsync(
            "culinaryblog",
            $"recipes/{recipeId}/images/{img1}.jpg",
            It.IsAny<CancellationToken>()), Times.Once);

        cacheMock.Verify(c => c.RemoveAsync("recipe:delete-img-recipe", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteImage_WhenImageNotFound_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "not-found-img-recipe", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new RecipeImageCommandHandler(db, cacheMock.Object);
        var command = new DeleteRecipeImageCommand(recipeId, Guid.NewGuid(), CurrentUserId: authorId);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
