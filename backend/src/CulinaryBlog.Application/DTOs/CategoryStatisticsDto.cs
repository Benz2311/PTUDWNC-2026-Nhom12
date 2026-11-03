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
)
{
    public CategoryStatisticsDto(
        int totalCategories,
        int totalRecipes,
        int publishedRecipes,
        int draftRecipes,
        CategoryStatisticItemDto[] categories)
        : this(totalCategories, totalRecipes, publishedRecipes, draftRecipes, 0, categories, [], [])
    {
    }
}

public record CategoryStatisticItemDto(
    Guid CategoryId,
    string CategoryName,
    int RecipeCount,
    decimal Percentage)
{
    public CategoryStatisticItemDto(Guid categoryId, string categoryName, int recipeCount)
        : this(categoryId, categoryName, recipeCount, 0)
    {
    }
}

public record RecipeMonthlyStatisticDto(
    int Year,
    int Month,
    int RecipeCount);

