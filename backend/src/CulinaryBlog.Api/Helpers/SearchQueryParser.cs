using System.Text.RegularExpressions;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Api.Helpers;

public static partial class SearchQueryParser
{
    [GeneratedRegex(@"\b(?:duoi|toi da|max|under)\s*(\d+)\s*(?:phut(?: nau)?|minutes?|mins?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MaxCookTimePattern();

    [GeneratedRegex(@"\b(?:it calories|it calo|low calories|it nang luong)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LowCaloriesPattern();

    public static ParsedSearchQuery Parse(string? rawQuery)
    {
        var clean = VietnameseTextNormalizer.Normalize(rawQuery ?? string.Empty);
        var maxTimeMatch = MaxCookTimePattern().Match(clean);
        var maxCookTime = maxTimeMatch.Success && int.TryParse(maxTimeMatch.Groups[1].Value, out var minutes)
            ? minutes
            : (int?)null;
        if (maxTimeMatch.Success) clean = MaxCookTimePattern().Replace(clean, " ");

        var lowCalories = LowCaloriesPattern().IsMatch(clean);
        if (lowCalories) clean = LowCaloriesPattern().Replace(clean, " ");
        clean = Regex.Replace(clean, @"\s+", " ").Trim();

        DifficultyLevel? difficulty = null;
        foreach (var (phrases, value) in new[]
        {
            (new[] { "de nau", "de lam", "easy" }, DifficultyLevel.Easy),
            (new[] { "trung binh", "medium" }, DifficultyLevel.Medium),
            (new[] { "kho nau", "kho lam", "hard" }, DifficultyLevel.Hard)
        })
        {
            var phrase = phrases.FirstOrDefault(item =>
                Regex.IsMatch(clean, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(item)}(?![\p{{L}}\p{{N}}])"));
            if (phrase is null) continue;
            difficulty = value;
            clean = Regex.Replace(clean, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(phrase)}(?![\p{{L}}\p{{N}}])", " ");
            clean = Regex.Replace(clean, @"\s+", " ").Trim();
            break;
        }

        var terms = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new ParsedSearchQuery(
            rawQuery ?? string.Empty,
            clean,
            clean,
            new ExtractedSearchFilters(maxCookTime, lowCalories ? 400m : null, difficulty?.ToString(), []),
            string.Join(" & ", terms));
    }

    public sealed record ParsedSearchQuery(
        string RawQuery,
        string CleanQuery,
        string UnaccentedQuery,
        ExtractedSearchFilters ExtractedFilters,
        string PostgresTsQuery);

    public sealed record ExtractedSearchFilters(
        int? MaxCookTime,
        decimal? MaxCalories,
        string? Difficulty,
        IReadOnlyList<string> IncludeIngredients);
}
