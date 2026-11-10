using CulinaryBlog.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace CulinaryBlog.UnitTests.Application.Features.Extended;

public class ExtendedFeaturesTests
{
    [Fact]
    public void CreateRecipeCollectionRequest_WithValidData_InstantiatesCorrectly()
    {
        var request = new CreateRecipeCollectionRequest("Món ngon cuối tuần", "Tuyển tập món ăn gia đình", false);

        request.Name.Should().Be("Món ngon cuối tuần");
        request.Description.Should().Be("Tuyển tập món ăn gia đình");
        request.IsPrivate.Should().BeFalse();
    }

    [Fact]
    public void TrendingRecipeDto_MappingProperties_MatchExpectedValues()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dto = new TrendingRecipeDto(
            1,
            id,
            "Phở bò Hà Nội",
            "pho-bo-ha-noi",
            "https://img.test/pho.jpg",
            "Món Nước",
            "mon-nuoc",
            "Bếp Trưởng",
            128,
            20,
            60,
            "Medium",
            now
        );

        dto.Rank.Should().Be(1);
        dto.Title.Should().Be("Phở bò Hà Nội");
        dto.ViewsInPeriod.Should().Be(128);
        dto.CategoryName.Should().Be("Món Nước");
    }

    [Fact]
    public void ActivityComparisonDto_CalculatesGrowthRateProperly()
    {
        var currentCreated = 20;
        var previousCreated = 10;
        var rate = previousCreated > 0
            ? Math.Round(((currentCreated - previousCreated) / (double)previousCreated) * 100, 1)
            : (currentCreated > 0 ? 100.0 : 0.0);

        var comparison = new ActivityComparisonDto(
            currentCreated,
            previousCreated,
            rate,
            15,
            5,
            200.0
        );

        comparison.CreatedGrowthRate.Should().Be(100.0);
        comparison.PublishedGrowthRate.Should().Be(200.0);
    }

    [Fact]
    public void CategoryPerformanceItemDto_WhenPreviousIsZero_SetsGrowthRateCorrectly()
    {
        var publishedCurrent = 8;
        var publishedPrevious = 0;
        var growthRate = publishedPrevious > 0
            ? Math.Round(((publishedCurrent - publishedPrevious) / (double)publishedPrevious) * 100, 1)
            : (publishedCurrent > 0 ? 100.0 : 0.0);

        var item = new CategoryPerformanceItemDto(
            Guid.NewGuid(),
            "Bánh ngọt",
            "banh-ngot",
            10,
            publishedCurrent,
            publishedPrevious,
            growthRate,
            350
        );

        item.GrowthRate.Should().Be(100.0);
        item.NewRecipesCount.Should().Be(10);
        item.TotalViewsInPeriod.Should().Be(350);
    }

    [Fact]
    public void DailyActivityStatDto_FormattedDate_MatchesExpectation()
    {
        var stat = new DailyActivityStatDto("2026-11-10", 5, 4);

        stat.Date.Should().Be("2026-11-10");
        stat.CreatedCount.Should().Be(5);
        stat.PublishedCount.Should().Be(4);
    }
}

