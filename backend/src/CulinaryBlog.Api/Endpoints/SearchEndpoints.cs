using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Api.Helpers;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace CulinaryBlog.Api.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/search").WithTags("Smart Search");
        group.MapGet("/recipes", SearchRecipes)
            .WithName("SmartSearchRecipes")
            .WithSummary("Tìm kiếm công thức với FTS, bộ lọc dinh dưỡng và nguyên liệu");
        group.MapGet("/suggestions", Suggestions)
            .WithName("RecipeSearchSuggestions")
            .WithSummary("Gợi ý từ khóa công thức");
    }

    private static async Task<IResult> SearchRecipes(
        string? q,
        ApplicationDbContext db,
        ICacheService cache,
        Guid? categoryId = null,
        string? categories = null,
        DifficultyLevel? difficulty = null,
        int? maxCookTime = null,
        decimal? minCalories = null,
        decimal? maxCalories = null,
        decimal? minProtein = null,
        decimal? maxProtein = null,
        decimal? minCarb = null,
        decimal? maxCarb = null,
        decimal? minFat = null,
        decimal? maxFat = null,
        decimal? minRating = null,
        string? includeIngredients = null,
        string? excludeIngredients = null,
        string sortBy = "relevance",
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var parsedQuery = SearchQueryParser.Parse(q);
        var normalized = parsedQuery.UnaccentedQuery;
        if (!string.IsNullOrWhiteSpace(q) && normalized.Length == 0 &&
            parsedQuery.ExtractedFilters.MaxCookTime is null &&
            parsedQuery.ExtractedFilters.MaxCalories is null &&
            parsedQuery.ExtractedFilters.Difficulty is null)
            return Results.Ok(new SearchResult([], new SearchMeta(page, pageSize, 0, 0), parsedQuery));
        var generation = await cache.GetAsync<string>("recipes:search:generation", cancellationToken) ?? "0";
        var cacheKey = "search:recipes:" + string.Join(':',
            generation, Uri.EscapeDataString(normalized), categoryId?.ToString("N"), Uri.EscapeDataString(categories ?? string.Empty),
            difficulty?.ToString(), maxCookTime, minCalories, maxCalories, minProtein, maxProtein, minCarb, maxCarb,
            minFat, maxFat, minRating, Uri.EscapeDataString(includeIngredients ?? string.Empty),
            Uri.EscapeDataString(excludeIngredients ?? string.Empty), sortBy, page, pageSize);
        var cached = await cache.GetAsync<SearchResult>(cacheKey, cancellationToken);
        if (cached is not null) return Results.Ok(cached);

        var query = db.Recipes.AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        (x.Status == RecipeStatus.Published || x.Status == RecipeStatus.Approved));

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            query = query.Where(x =>
                EF.Property<NpgsqlTsVector>(x, "SearchVectorFts")
                    .Matches(EF.Functions.PlainToTsQuery("simple", normalized)) ||
                EF.Functions.Unaccent(x.Title).ToLower().Contains(normalized) ||
                EF.Functions.Unaccent(x.Description).ToLower().Contains(normalized) ||
                EF.Functions.Unaccent(x.Content).ToLower().Contains(normalized) ||
                x.Ingredients.Any(i => EF.Functions.Unaccent(i.Name).ToLower().Contains(normalized)) ||
                x.Steps.Any(step => EF.Functions.Unaccent(step.Title + " " + step.Description).ToLower().Contains(normalized)));
        }

        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        var categoryIds = (categories ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var parsed) ? parsed : (Guid?)null)
            .Where(value => value.HasValue).Select(value => value!.Value).Distinct().ToArray();
        if (categoryIds.Length > 0) query = query.Where(x => categoryIds.Contains(x.CategoryId));
        maxCookTime ??= parsedQuery.ExtractedFilters.MaxCookTime;
        difficulty ??= Enum.TryParse<DifficultyLevel>(parsedQuery.ExtractedFilters.Difficulty, out var parsedDifficulty)
            ? parsedDifficulty
            : null;
        maxCalories ??= parsedQuery.ExtractedFilters.MaxCalories;
        if (difficulty.HasValue) query = query.Where(x => x.Difficulty == difficulty.Value);
        if (maxCookTime is > 0) query = query.Where(x => x.CookTimeMinutes <= maxCookTime);
        if (minCalories.HasValue) query = query.Where(x => x.Nutrition.Calories >= minCalories);
        if (maxCalories.HasValue) query = query.Where(x => x.Nutrition.Calories <= maxCalories);
        if (minProtein.HasValue) query = query.Where(x => x.Nutrition.Protein >= minProtein);
        if (maxProtein.HasValue) query = query.Where(x => x.Nutrition.Protein <= maxProtein);
        if (minCarb.HasValue) query = query.Where(x => x.Nutrition.Carbohydrates >= minCarb);
        if (maxCarb.HasValue) query = query.Where(x => x.Nutrition.Carbohydrates <= maxCarb);
        if (minFat.HasValue) query = query.Where(x => x.Nutrition.Fat >= minFat);
        if (maxFat.HasValue) query = query.Where(x => x.Nutrition.Fat <= maxFat);
        if (minRating.HasValue) query = query.Where(x =>
            db.RecipeRatings.Where(r => r.RecipeId == x.Id && !r.IsDeleted).Average(r => (double?)r.Score) >= (double)minRating);

        foreach (var ingredient in Split(includeIngredients))
            query = query.Where(x => x.Ingredients.Any(i => EF.Functions.Unaccent(i.Name).ToLower().Contains(ingredient)));
        foreach (var ingredient in Split(excludeIngredients))
            query = query.Where(x => !x.Ingredients.Any(i => EF.Functions.Unaccent(i.Name).ToLower().Contains(ingredient)));

        var total = await query.CountAsync(cancellationToken);
        var ordered = sortBy.ToLowerInvariant() switch
        {
            "newest" => query.OrderByDescending(x => x.PublishedAt),
            "cooktime" => query.OrderBy(x => x.CookTimeMinutes),
            "rating" => query.OrderByDescending(x =>
                db.RecipeRatings.Where(r => r.RecipeId == x.Id && !r.IsDeleted).Average(r => (double?)r.Score) ?? 0),
            _ when !string.IsNullOrWhiteSpace(normalized) => query.OrderByDescending(x =>
                EF.Property<NpgsqlTsVector>(x, "SearchVectorFts")
                    .Rank(EF.Functions.PlainToTsQuery("simple", normalized))),
            _ => query.OrderByDescending(x => x.PublishedAt)
        };

        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new SearchItem(
                x.Id,
                x.Title,
                x.Slug,
                x.Description,
                x.PrepTimeMinutes,
                x.CookTimeMinutes,
                x.Servings,
                x.Difficulty,
                x.Status,
                x.PublishedAt,
                x.Author.DisplayName,
                x.CategoryId,
                x.Category.Name,
                db.RecipeRatings.Where(r => r.RecipeId == x.Id && !r.IsDeleted).Average(r => (double?)r.Score) ?? 0,
                x.Images.Where(i => i.IsPrimary && !i.IsDeleted).Select(i => i.OriginalUrl).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var result = new SearchResult(
            items,
            new SearchMeta(page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize)),
            parsedQuery);
        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> Suggestions(
        string? q,
        ApplicationDbContext db,
        ICacheService cache,
        int limit = 8,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) return Results.Ok(new { data = Array.Empty<string>() });
        var term = VietnameseTextNormalizer.Normalize(q);
        var generation = await cache.GetAsync<string>("recipes:search:generation", cancellationToken) ?? "0";
        var cacheKey = $"search:suggestions:{generation}:{Uri.EscapeDataString(term)}:{Math.Clamp(limit, 1, 5)}";
        var cached = await cache.GetAsync<IReadOnlyList<string>>(cacheKey, cancellationToken);
        if (cached is not null) return Results.Ok(new { data = cached });
        var suggestions = await db.Recipes.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == RecipeStatus.Published &&
                EF.Functions.Unaccent(x.Title).ToLower().Contains(term))
            .OrderByDescending(x => x.PublishedAt).Select(x => x.Title)
            .Distinct().Take(Math.Clamp(limit, 1, 5)).ToListAsync(cancellationToken);
        await cache.SetAsync(cacheKey, (IReadOnlyList<string>)suggestions, TimeSpan.FromMinutes(15), cancellationToken);
        return Results.Ok(new { data = suggestions });
    }

    private static IEnumerable<string> Split(string? values) =>
        (values ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(VietnameseTextNormalizer.Normalize);

    private sealed record SearchMeta(int Page, int PageSize, int Total, int TotalPages);
    private sealed record SearchItem(
        Guid Id,
        string Title,
        string Slug,
        string Description,
        int PrepTimeMinutes,
        int CookTimeMinutes,
        int Servings,
        DifficultyLevel Difficulty,
        RecipeStatus Status,
        DateTime? PublishedAt,
        string AuthorName,
        Guid CategoryId,
        string Category,
        double Rating,
        string? ImageUrl);
    private sealed record SearchResult(
        IReadOnlyList<SearchItem> Data,
        SearchMeta Meta,
        SearchQueryParser.ParsedSearchQuery ParsedQuery);
}
