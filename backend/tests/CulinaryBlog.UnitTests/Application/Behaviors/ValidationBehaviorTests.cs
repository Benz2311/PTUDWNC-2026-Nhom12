using CulinaryBlog.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Behaviors;

// Stub request và response cho testing
public record TestRequest(string Value) : IRequest<string>;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestRequest, string>(
            Enumerable.Empty<IValidator<TestRequest>>());

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        // Act
        var result = await behavior.Handle(new TestRequest("test"), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidatorPasses_CallsNext()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>(
            new[] { validatorMock.Object });

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult("success");
        };

        // Act
        var result = await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be("success");
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Value", "Value must not be empty"),
            new("Value", "Value must be at least 3 characters")
        };

        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, string>(
            new[] { validatorMock.Object });

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("should_not_reach");

        // Act
        var act = async () => await behavior.Handle(
            new TestRequest(""), next, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*Value must not be empty*");
    }

    [Fact]
    public async Task Handle_WhenMultipleValidators_AllAreExecuted()
    {
        // Arrange
        var validator1 = new Mock<IValidator<TestRequest>>();
        var validator2 = new Mock<IValidator<TestRequest>>();

        validator1
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        validator2
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>(
            new[] { validator1.Object, validator2.Object });

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("ok");

        // Act
        await behavior.Handle(new TestRequest("test"), next, CancellationToken.None);

        // Assert
        validator1.Verify(
            v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        validator2.Verify(
            v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
