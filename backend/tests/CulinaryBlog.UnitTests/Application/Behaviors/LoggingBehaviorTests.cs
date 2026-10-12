using System.Diagnostics;
using CulinaryBlog.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Behaviors;

public sealed class LoggingBehaviorTests
{
    public sealed record SampleSensitiveCommand(string Username, string Password, string AccessToken)
        : IRequest<string>;

    public sealed record SlowQuery(int DelayMs)
        : IRequest<string>;

    [Fact]
    public async Task LoggingBehavior_LogsRequestNameAndDuration_OnSuccess()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LoggingBehavior<SampleSensitiveCommand, string>>>();
        var behavior = new LoggingBehavior<SampleSensitiveCommand, string>(loggerMock.Object);
        var command = new SampleSensitiveCommand("chef_manh", "superSecret123!", "jwt.header.payload.signature");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("SuccessResult");

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.Equal("SuccessResult", result);

        // Verify handling log call
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling MediatR request SampleSensitiveCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify handled log call
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled MediatR request SampleSensitiveCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoggingBehavior_LogsWarning_WhenExecutionExceeds500ms()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LoggingBehavior<SlowQuery, string>>>();
        var behavior = new LoggingBehavior<SlowQuery, string>(loggerMock.Object);
        var query = new SlowQuery(550);

        RequestHandlerDelegate<string> next = async (ct) =>
        {
            await Task.Delay(550, ct);
            return "SlowCompleted";
        };

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        Assert.Equal("SlowCompleted", result);

        // Verify slow request warning (> 500ms)
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Long-running MediatR request SlowQuery completed in")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoggingBehavior_DoesNotLogWarning_WhenExecutionUnder500ms()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LoggingBehavior<SlowQuery, string>>>();
        var behavior = new LoggingBehavior<SlowQuery, string>(loggerMock.Object);
        var query = new SlowQuery(10);

        RequestHandlerDelegate<string> next = async (ct) =>
        {
            await Task.Delay(10, ct);
            return "FastCompleted";
        };

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        Assert.Equal("FastCompleted", result);

        // Verify Warning is NEVER logged
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task LoggingBehavior_LogsErrorAndRethrows_WhenHandlerThrowsException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LoggingBehavior<SampleSensitiveCommand, string>>>();
        var behavior = new LoggingBehavior<SampleSensitiveCommand, string>(loggerMock.Object);
        var command = new SampleSensitiveCommand("user", "pass", "token");
        var expectedException = new InvalidOperationException("Database connection timeout");

        RequestHandlerDelegate<string> next = (ct) => throw expectedException;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(command, next, CancellationToken.None));

        Assert.Same(expectedException, ex);

        // Verify error log
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error handling MediatR request SampleSensitiveCommand")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoggingBehavior_DoesNotLogSensitiveData_SuchAsPasswordsOrTokens()
    {
        // Arrange
        var loggedMessages = new List<string>();
        var loggerMock = new Mock<ILogger<LoggingBehavior<SampleSensitiveCommand, string>>>();

        loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                var state = invocation.Arguments[2];
                if (state != null)
                {
                    loggedMessages.Add(state.ToString()!);
                }
            }));

        var behavior = new LoggingBehavior<SampleSensitiveCommand, string>(loggerMock.Object);
        var command = new SampleSensitiveCommand("super_chef", "SecretP@ssw0rd999!", "eyJh.eyJzdWI.secretSig");

        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Done");

        // Act
        await behavior.Handle(command, next, CancellationToken.None);

        // Assert: Không bao giờ xuất hiện password hoặc token trong bất kỳ log message nào
        foreach (var msg in loggedMessages)
        {
            Assert.DoesNotContain("SecretP@ssw0rd999!", msg);
            Assert.DoesNotContain("secretSig", msg);
        }
    }
}
