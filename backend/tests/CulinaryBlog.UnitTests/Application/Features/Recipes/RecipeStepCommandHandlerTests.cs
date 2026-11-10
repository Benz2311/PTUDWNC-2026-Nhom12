using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeSteps;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Recipes;

public class RecipeStepCommandHandlerTests
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
    public async Task CreateStep_WhenValid_AddsStepWithIncrementedNumberAndInvalidatesCache()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Slug = "recipe-step-test",
            Title = "Recipe For Step Test",
            IsDeleted = false
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        var command = new CreateRecipeStepCommand(
            RecipeId: recipe.Id,
            Title: "Step 1 Title",
            Description: "Mix ingredients thoroughly",
            TimerMinutes: 15,
            ImageUrl: "https://minio.local/step1.jpg",
            CurrentUserId: authorId,
            IsAdmin: false
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.StepNumber.Should().Be(1);
        result.Title.Should().Be("Step 1 Title");
        result.Description.Should().Be("Mix ingredients thoroughly");
        result.TimerMinutes.Should().Be(15);
        result.ImageUrl.Should().Be("https://minio.local/step1.jpg");

        var dbStep = db.RecipeSteps.FirstOrDefault(s => s.RecipeId == recipe.Id);
        dbStep.Should().NotBeNull();
        dbStep!.StepNumber.Should().Be(1);
        cacheMock.Verify(c => c.RemoveAsync("recipe:recipe-step-test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStep_WhenMultipleStepsExist_AssignsNextMaxStepNumber()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = recipeId,
            AuthorId = authorId,
            Slug = "recipe-multi-step",
            IsDeleted = false
        };
        db.Recipes.Add(recipe);
        db.RecipeSteps.AddRange(
            new RecipeStep { Id = Guid.NewGuid(), RecipeId = recipeId, StepNumber = 1, Description = "S1", IsDeleted = false },
            new RecipeStep { Id = Guid.NewGuid(), RecipeId = recipeId, StepNumber = 5, Description = "S5", IsDeleted = false },
            new RecipeStep { Id = Guid.NewGuid(), RecipeId = recipeId, StepNumber = 10, Description = "Deleted", IsDeleted = true }
        );
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        var command = new CreateRecipeStepCommand(recipeId, "New Step", "Next step description", CurrentUserId: authorId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.StepNumber.Should().Be(6); // 5 + 1
    }

    [Theory]
    [InlineData("", "Valid Title", 10, null, "Description is required")]
    [InlineData("   ", "Valid Title", 10, null, "Description is required")]
    [InlineData("Valid description", "Over 200 chars ...", 10, null, "Title cannot exceed 200")]
    [InlineData("Valid description", "Valid Title", -5, null, "TimerMinutes must be greater than or equal to 0")]
    public async Task CreateStep_WhenFieldsInvalid_ThrowsValidationException(string desc, string title, int timer, string? url, string expectedErrorSubstring)
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);

        if (title.StartsWith("Over 200"))
        {
            title = new string('A', 205);
        }

        var command = new CreateRecipeStepCommand(Guid.NewGuid(), title, desc, timer, url);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Values.SelectMany(v => v).Should().Contain(msg => msg.Contains(expectedErrorSubstring));
    }

    [Fact]
    public async Task CreateStep_WhenRecipeNotFound_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        var command = new CreateRecipeStepCommand(Guid.NewGuid(), "Title", "Description", CurrentUserId: Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateStep_WhenUserIsNotAuthorAndNotAdmin_ThrowsForbiddenException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipe = new Recipe { Id = Guid.NewGuid(), AuthorId = authorId, Slug = "not-my-recipe", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        var command = new CreateRecipeStepCommand(recipe.Id, "Title", "Description", CurrentUserId: Guid.NewGuid(), IsAdmin: false);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateStep_WhenUserIsAdmin_AllowsCreationEvenIfNotAuthor()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipe = new Recipe { Id = Guid.NewGuid(), AuthorId = authorId, Slug = "admin-step-test", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        var command = new CreateRecipeStepCommand(recipe.Id, "Admin Step", "Admin description", CurrentUserId: Guid.NewGuid(), IsAdmin: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Description.Should().Be("Admin description");
    }

    [Fact]
    public async Task UpdateStep_WhenValid_UpdatesFieldsPreservingStepNumber()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "update-step-test", IsDeleted = false };
        var step = new RecipeStep
        {
            Id = stepId,
            RecipeId = recipeId,
            StepNumber = 3,
            Title = "Old Title",
            Description = "Old Description",
            TimerMinutes = 5,
            IsDeleted = false
        };
        db.Recipes.Add(recipe);
        db.RecipeSteps.Add(step);
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        var command = new UpdateRecipeStepCommand(recipeId, stepId, "New Title", "New Description", 10, "https://image.local/new.png", authorId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.StepNumber.Should().Be(3); // StepNumber must remain 3
        result.Title.Should().Be("New Title");
        result.Description.Should().Be("New Description");
        result.TimerMinutes.Should().Be(10);
        result.ImageUrl.Should().Be("https://image.local/new.png");

        var dbStep = db.RecipeSteps.Find(stepId);
        dbStep!.StepNumber.Should().Be(3);
        dbStep.Title.Should().Be("New Title");
        cacheMock.Verify(c => c.RemoveAsync("recipe:update-step-test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStep_WhenStepBelongsToDifferentRecipe_ThrowsValidationException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId1 = Guid.NewGuid();
        var recipeId2 = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        db.Recipes.AddRange(
            new Recipe { Id = recipeId1, AuthorId = authorId, Slug = "recipe-1", IsDeleted = false },
            new Recipe { Id = recipeId2, AuthorId = authorId, Slug = "recipe-2", IsDeleted = false }
        );
        db.RecipeSteps.Add(new RecipeStep { Id = stepId, RecipeId = recipeId2, StepNumber = 1, Description = "S2", IsDeleted = false });
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        var command = new UpdateRecipeStepCommand(recipeId1, stepId, "Title", "Desc", CurrentUserId: authorId);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("StepId");
    }

    [Fact]
    public async Task DeleteStep_WhenValid_SoftDeletesAndRenumbersRemainingStepsContinuously()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var step1Id = Guid.NewGuid();
        var step2Id = Guid.NewGuid();
        var step3Id = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "del-step-test", IsDeleted = false };
        db.Recipes.Add(recipe);
        db.RecipeSteps.AddRange(
            new RecipeStep { Id = step1Id, RecipeId = recipeId, StepNumber = 1, Description = "Step 1", IsDeleted = false },
            new RecipeStep { Id = step2Id, RecipeId = recipeId, StepNumber = 2, Description = "Step 2 (Delete me)", IsDeleted = false },
            new RecipeStep { Id = step3Id, RecipeId = recipeId, StepNumber = 3, Description = "Step 3", IsDeleted = false }
        );
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        var command = new DeleteRecipeStepCommand(recipeId, step2Id, CurrentUserId: authorId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();

        var deletedStep = db.RecipeSteps.Find(step2Id);
        deletedStep!.IsDeleted.Should().BeTrue();

        var remaining = db.RecipeSteps.Where(s => s.RecipeId == recipeId && !s.IsDeleted).OrderBy(s => s.StepNumber).ToList();
        remaining.Should().HaveCount(2);
        remaining[0].Id.Should().Be(step1Id);
        remaining[0].StepNumber.Should().Be(1);
        remaining[1].Id.Should().Be(step3Id);
        remaining[1].StepNumber.Should().Be(2); // Was 3, continuously renumbered to 2!

        cacheMock.Verify(c => c.RemoveAsync("recipe:del-step-test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReorderSteps_WhenValid_ReordersAllStepsSequentially()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var step1Id = Guid.NewGuid();
        var step2Id = Guid.NewGuid();
        var step3Id = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "reorder-step-test", IsDeleted = false };
        db.Recipes.Add(recipe);
        db.RecipeSteps.AddRange(
            new RecipeStep { Id = step1Id, RecipeId = recipeId, StepNumber = 1, Description = "Step 1", IsDeleted = false },
            new RecipeStep { Id = step2Id, RecipeId = recipeId, StepNumber = 2, Description = "Step 2", IsDeleted = false },
            new RecipeStep { Id = step3Id, RecipeId = recipeId, StepNumber = 3, Description = "Step 3", IsDeleted = false }
        );
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);
        // Reverse order: 3, 1, 2
        var command = new ReorderRecipeStepsCommand(recipeId, [step3Id, step1Id, step2Id], CurrentUserId: authorId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Id.Should().Be(step3Id);
        result[0].StepNumber.Should().Be(1);
        result[1].Id.Should().Be(step1Id);
        result[1].StepNumber.Should().Be(2);
        result[2].Id.Should().Be(step2Id);
        result[2].StepNumber.Should().Be(3);

        cacheMock.Verify(c => c.RemoveAsync("recipe:reorder-step-test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReorderSteps_WhenCountMismatchOrDuplicates_ThrowsValidationException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "reorder-err", IsDeleted = false };
        db.Recipes.Add(recipe);
        db.RecipeSteps.AddRange(
            new RecipeStep { Id = s1, RecipeId = recipeId, StepNumber = 1, Description = "S1", IsDeleted = false },
            new RecipeStep { Id = s2, RecipeId = recipeId, StepNumber = 2, Description = "S2", IsDeleted = false }
        );
        await db.SaveChangesAsync();

        var handler = new RecipeStepCommandHandler(db, cacheMock.Object);

        // Mismatch count (only 1 step passed)
        var cmd1 = new ReorderRecipeStepsCommand(recipeId, [s1], CurrentUserId: authorId);
        var act1 = async () => await handler.Handle(cmd1, CancellationToken.None);
        await act1.Should().ThrowAsync<ValidationException>();

        // Duplicate step IDs
        var cmd2 = new ReorderRecipeStepsCommand(recipeId, [s1, s1], CurrentUserId: authorId);
        var act2 = async () => await handler.Handle(cmd2, CancellationToken.None);
        await act2.Should().ThrowAsync<ValidationException>();

        // Unknown step ID
        var cmd3 = new ReorderRecipeStepsCommand(recipeId, [s1, Guid.NewGuid()], CurrentUserId: authorId);
        var act3 = async () => await handler.Handle(cmd3, CancellationToken.None);
        await act3.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UploadStepImage_WhenValidStream_UploadsToStorageAndReturnsUrl()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var storageMock = new Mock<IStorageService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "upload-step-img", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        storageMock.Setup(s => s.UploadAsync(
            "culinaryblog",
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            "image/jpeg",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://minio:9000/culinaryblog/recipes/step_uploaded.jpg");

        using var memoryStream = new MemoryStream(ValidJpegBytes);
        var handler = new RecipeStepCommandHandler(db, cacheMock.Object, storageMock.Object);
        var command = new UploadRecipeStepImageCommand(
            recipeId,
            memoryStream,
            "step.jpg",
            "image/jpeg",
            ValidJpegBytes.Length,
            CurrentUserId: authorId
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Url.Should().Be("http://minio:9000/culinaryblog/recipes/step_uploaded.jpg");
        storageMock.Verify(s => s.UploadAsync(
            "culinaryblog",
            It.Is<string>(key => key.StartsWith($"recipes/{recipeId}/steps/")),
            It.IsAny<Stream>(),
            "image/jpeg",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadStepImage_WhenInvalidImage_ThrowsValidationException()
    {
        using var db = TestDbContextFactory.CreateInMemoryContext();
        var cacheMock = new Mock<ICacheService>();
        var storageMock = new Mock<IStorageService>();
        var authorId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var recipe = new Recipe { Id = recipeId, AuthorId = authorId, Slug = "upload-step-bad", IsDeleted = false };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        using var badStream = new MemoryStream([0x00, 0x01, 0x02]); // Corrupt / unknown header
        var handler = new RecipeStepCommandHandler(db, cacheMock.Object, storageMock.Object);
        var command = new UploadRecipeStepImageCommand(
            recipeId,
            badStream,
            "malicious.exe",
            "application/octet-stream",
            3,
            CurrentUserId: authorId
        );

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
