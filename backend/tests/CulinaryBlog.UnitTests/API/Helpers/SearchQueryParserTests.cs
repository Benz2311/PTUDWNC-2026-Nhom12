using CulinaryBlog.Api.Helpers;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.UnitTests.Api.Helpers;

public sealed class SearchQueryParserTests
{
    [Fact]
    public void Parse_ExtractsImplicitCookTimeAndLowCalories()
    {
        var parsed = SearchQueryParser.Parse("canh chua ca loc duoi 30 phut it calories");

        Assert.Equal("canh chua ca loc", parsed.CleanQuery);
        Assert.Equal(30, parsed.ExtractedFilters.MaxCookTime);
        Assert.Equal(400m, parsed.ExtractedFilters.MaxCalories);
    }

    [Fact]
    public void Parse_DoesNotTreatCookingMethodAsDifficulty()
    {
        var parsed = SearchQueryParser.Parse("ca kho duoi 20 phut");

        Assert.Equal("ca kho", parsed.CleanQuery);
        Assert.Equal(20, parsed.ExtractedFilters.MaxCookTime);
        Assert.Null(parsed.ExtractedFilters.Difficulty);
    }

    [Theory]
    [InlineData("mon de lam", DifficultyLevel.Easy)]
    [InlineData("mon kho lam", DifficultyLevel.Hard)]
    public void Parse_ExtractsExplicitDifficulty(string query, DifficultyLevel expected)
    {
        var parsed = SearchQueryParser.Parse(query);

        Assert.Equal(expected.ToString(), parsed.ExtractedFilters.Difficulty);
    }
}
