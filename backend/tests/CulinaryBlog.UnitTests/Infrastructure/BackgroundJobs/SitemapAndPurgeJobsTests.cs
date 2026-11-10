using System.Reflection;
using CulinaryBlog.Api.Jobs;
using CulinaryBlog.Application.Interfaces;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CulinaryBlog.UnitTests.Infrastructure.BackgroundJobs;

public class SitemapAndPurgeJobsTests
{
    [Fact]
    public void SitemapGenerationJob_HasAutomaticRetryAttribute_With2Attempts()
    {
        var method = typeof(SitemapGenerationJob).GetMethod(nameof(SitemapGenerationJob.RunAsync));
        Assert.NotNull(method);

        var retryAttr = method.GetCustomAttribute<AutomaticRetryAttribute>();
        Assert.NotNull(retryAttr);
        Assert.Equal(2, retryAttr.Attempts);
    }

    [Fact]
    public async Task SitemapGenerationJob_CallsGeneratorAndCompletesSuccessfully()
    {
        var mockGenerator = new Mock<ISitemapGenerator>();
        mockGenerator.Setup(g => g.GenerateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("<urlset><url><loc>http://localhost:8088/recipes/test</loc></url></urlset>");

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Site:PublicBaseUrl"] = "http://localhost:8088",
            ["GoogleSearch:PingUrlTemplate"] = null // No ping in unit test
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var mockLogger = new Mock<ILogger<SitemapGenerationJob>>();

        var job = new SitemapGenerationJob(mockGenerator.Object, configuration, mockLogger.Object);
        await job.RunAsync(CancellationToken.None);

        mockGenerator.Verify(g => g.GenerateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void PurgeSoftDeletedJob_HasAutomaticRetryAttribute_With2Attempts()
    {
        var method = typeof(PurgeSoftDeletedJob).GetMethod(nameof(PurgeSoftDeletedJob.RunAsync));
        Assert.NotNull(method);

        var retryAttr = method.GetCustomAttribute<AutomaticRetryAttribute>();
        Assert.NotNull(retryAttr);
        Assert.Equal(2, retryAttr.Attempts);
    }

    [Fact]
    public async Task PurgeSoftDeletedJob_CallsPurgeExpiredAsync_AndCompletes()
    {
        var mockPurgeService = new Mock<IRecipePurgeService>();
        mockPurgeService.Setup(p => p.PurgeExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var mockLogger = new Mock<ILogger<PurgeSoftDeletedJob>>();
        var job = new PurgeSoftDeletedJob(mockPurgeService.Object, mockLogger.Object);

        await job.RunAsync(CancellationToken.None);

        mockPurgeService.Verify(p => p.PurgeExpiredAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
