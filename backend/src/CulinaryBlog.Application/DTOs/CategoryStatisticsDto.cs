namespace CulinaryBlog.Application.DTOs;

public record CategoryStatisticsDto(
    int TotalCategories,
    int TotalRecipes,
    int PublishedRecipes,
    int DraftRecipes,
    CategoryStatisticItemDto[] Categories
);

public record CategoryStatisticItemDto(
    Guid CategoryId,
    string CategoryName,
    int RecipeCount
);

