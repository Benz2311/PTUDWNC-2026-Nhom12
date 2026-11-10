namespace CulinaryBlog.Application.DTOs;

public record RecipeCollectionDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsPrivate,
    Guid UserId,
    string UserName,
    int RecipeCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<string> PreviewImageUrls);

public record RecipeCollectionItemDto(
    Guid RecipeId,
    string Title,
    string Slug,
    string? CoverImageUrl,
    string CategoryName,
    string CategorySlug,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    string Difficulty,
    DateTime AddedAt);

public record RecipeCollectionDetailDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsPrivate,
    Guid UserId,
    string UserName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<RecipeCollectionItemDto> Recipes);

public record CreateRecipeCollectionRequest(
    string Name,
    string? Description,
    bool IsPrivate);

public record UpdateRecipeCollectionRequest(
    string Name,
    string? Description,
    bool IsPrivate);

public record AddRecipeToCollectionRequest(
    Guid RecipeId);

public record TrendingRecipeDto(
    int Rank,
    Guid RecipeId,
    string Title,
    string Slug,
    string? CoverImageUrl,
    string CategoryName,
    string CategorySlug,
    string AuthorName,
    int ViewsInPeriod,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    string Difficulty,
    DateTime? PublishedAt);

public record DailyActivityStatDto(
    string Date,
    int CreatedCount,
    int PublishedCount);

public record ActivityComparisonDto(
    int TotalCreatedCurrent,
    int TotalCreatedPrevious,
    double CreatedGrowthRate,
    int TotalPublishedCurrent,
    int TotalPublishedPrevious,
    double PublishedGrowthRate);

public record TopAuthorActivityDto(
    Guid AuthorId,
    string AuthorName,
    string AuthorEmail,
    string? AvatarUrl,
    int RecipesCreated,
    int RecipesPublished);

public record ActivityAnalyticsDto(
    int Days,
    IReadOnlyList<DailyActivityStatDto> DailyStats,
    ActivityComparisonDto Summary,
    IReadOnlyList<TopAuthorActivityDto> TopAuthors);

public record CategoryPerformanceItemDto(
    Guid CategoryId,
    string CategoryName,
    string Slug,
    int NewRecipesCount,
    int PublishedRecipesCount,
    int PreviousPublishedCount,
    double GrowthRate,
    int TotalViewsInPeriod);

public record CategoryPerformanceAnalyticsDto(
    int Days,
    IReadOnlyList<CategoryPerformanceItemDto> Categories,
    IReadOnlyList<CategoryPerformanceItemDto> FastestGrowingCategories,
    IReadOnlyList<CategoryPerformanceItemDto> LowActivityCategories);

