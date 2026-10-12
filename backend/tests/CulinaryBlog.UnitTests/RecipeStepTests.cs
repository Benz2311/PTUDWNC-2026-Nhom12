using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Commands.RecipeSteps;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests;

public sealed class RecipeStepTests
{
    private static ApplicationDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void RecipeStepModel_EnforcesStepNumberAndTimerCheckConstraints()
    {
        using var context = CreateModelContext();
        var designModel = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>(context).Model;
        var entityType = designModel.FindEntityType(typeof(RecipeStep))!;

        var checkConstraints = entityType.GetCheckConstraints().ToList();
        checkConstraints.Should().Contain(c => c.Name == "CK_RecipeSteps_StepNumber" && c.Sql == "\"StepNumber\" >= 1");
        checkConstraints.Should().Contain(c => c.Name == "CK_RecipeSteps_TimerMinutes" && c.Sql == "\"TimerMinutes\" IS NULL OR \"TimerMinutes\" >= 0");
    }

    [Fact]
    public void RecipeStepModel_HasFilteredUniqueIndexOnRecipeIdAndStepNumber()
    {
        using var context = CreateModelContext();
        var entityType = context.Model.FindEntityType(typeof(RecipeStep))!;

        var index = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecipeStep.RecipeId), nameof(RecipeStep.StepNumber) }));

        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("\"IsDeleted\" = false");
    }

    [Fact]
    public void RecipeStepModel_ConfiguresPropertyLengthsAndRequirements()
    {
        using var context = CreateModelContext();
        var entityType = context.Model.FindEntityType(typeof(RecipeStep))!;

        var titleProp = entityType.FindProperty(nameof(RecipeStep.Title));
        titleProp.Should().NotBeNull();
        titleProp!.GetMaxLength().Should().Be(200);

        var descProp = entityType.FindProperty(nameof(RecipeStep.Description));
        descProp.Should().NotBeNull();
        descProp!.IsNullable.Should().BeFalse();

        var imageProp = entityType.FindProperty(nameof(RecipeStep.ImageUrl));
        imageProp.Should().NotBeNull();
        imageProp!.GetMaxLength().Should().Be(500);
    }

    [Fact]
    public async Task CreateStep_ThrowsValidationException_WhenDescriptionIsEmpty()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var cacheMock = new Mock<ICacheService>();
        var handler = new RecipeStepCommandHandler(dbMock.Object, cacheMock.Object);

        var command = new CreateRecipeStepCommand(Guid.NewGuid(), "Valid Title", "", 10);

        var act = () => handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Description");
    }

    [Fact]
    public async Task CreateStep_ThrowsValidationException_WhenTitleExceeds200Characters()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var cacheMock = new Mock<ICacheService>();
        var handler = new RecipeStepCommandHandler(dbMock.Object, cacheMock.Object);

        var longTitle = new string('A', 201);
        var command = new CreateRecipeStepCommand(Guid.NewGuid(), longTitle, "Valid description", 10);

        var act = () => handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task CreateStep_ThrowsValidationException_WhenTimerMinutesIsNegative()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var cacheMock = new Mock<ICacheService>();
        var handler = new RecipeStepCommandHandler(dbMock.Object, cacheMock.Object);

        var command = new CreateRecipeStepCommand(Guid.NewGuid(), "Title", "Description", -1);

        var act = () => handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("TimerMinutes");
    }

    [Fact]
    public async Task CreateStep_ThrowsValidationException_WhenImageUrlExceeds500Characters()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var cacheMock = new Mock<ICacheService>();
        var handler = new RecipeStepCommandHandler(dbMock.Object, cacheMock.Object);

        var longUrl = "https://example.com/" + new string('x', 500);
        var command = new CreateRecipeStepCommand(Guid.NewGuid(), "Title", "Description", 10, longUrl);

        var act = () => handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("ImageUrl");
    }

    [Fact]
    public async Task UpdateStep_ThrowsValidationException_WhenDescriptionIsEmpty()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var cacheMock = new Mock<ICacheService>();
        var handler = new RecipeStepCommandHandler(dbMock.Object, cacheMock.Object);

        var command = new UpdateRecipeStepCommand(Guid.NewGuid(), Guid.NewGuid(), "Title", "   ", 10);

        var act = () => handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Description");
    }

    [Fact]
    public async Task UpdateStep_ThrowsValidationException_WhenTimerMinutesIsNegative()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var cacheMock = new Mock<ICacheService>();
        var handler = new RecipeStepCommandHandler(dbMock.Object, cacheMock.Object);

        var command = new UpdateRecipeStepCommand(Guid.NewGuid(), Guid.NewGuid(), "Title", "Valid Description", -5);

        var act = () => handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("TimerMinutes");
    }
}
