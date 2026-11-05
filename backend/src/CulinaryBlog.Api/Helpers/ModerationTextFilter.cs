using System.Text.RegularExpressions;
using CulinaryBlog.Application.Common.Utilities;

namespace CulinaryBlog.Api.Helpers;

public static class ModerationTextFilter
{
    private static readonly string[] BlockedTerms =
    [
        "địt", "đụ", "fuck", "shit", "porn", "sex"
    ];

    private static readonly Regex HtmlTag = new("<\\s*/?\\s*[a-z][^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RepeatedCharacter = new("(.)\\1{9,}", RegexOptions.Compiled);
    private static readonly Regex Url = new(@"https?://", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool ContainsBlockedTerm(string value)
    {
        var normalized = VietnameseTextNormalizer.Normalize(value);
        return BlockedTerms.Any(term => Regex.IsMatch(
            normalized,
            $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(VietnameseTextNormalizer.Normalize(term))}(?![\p{{L}}\p{{N}}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    public static string? FindViolation(string value)
    {
        if (ContainsBlockedTerm(value)) return "BLOCKED_LANGUAGE";
        if (HtmlTag.IsMatch(value)) return "HTML_NOT_ALLOWED";
        if (RepeatedCharacter.IsMatch(value) || Url.Matches(value).Count > 2) return "SPAM_DETECTED";
        return null;
    }
}
