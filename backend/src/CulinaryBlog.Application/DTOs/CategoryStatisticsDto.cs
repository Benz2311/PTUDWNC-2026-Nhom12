namespace CulinaryBlog.Application.DTOs;

public record CategoryStatisticsDto(
    int TotalCategories,
    int TotalRecipes,
    int PublishedRecipes,
    int DraftRecipes,
    int ArchivedRecipes,
    CategoryStatisticItemDto[] Categories,
    CategoryStatisticItemDto[] TopCategories,
    RecipeMonthlyStatisticDto[] RecipesByMonth
);

public record CategoryStatisticItemDto(
    Guid CategoryId,
    string CategoryName,
    int RecipeCount,
    decimal Percentage);

public record RecipeMonthlyStatisticDto(
    int Year,
    int Month,
    int RecipeCount)
{
    public int Count => RecipeCount;
}

