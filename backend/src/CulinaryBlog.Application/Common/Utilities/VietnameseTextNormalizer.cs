using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CulinaryBlog.Application.Common.Utilities;

public static class VietnameseTextNormalizer
{
    private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Chuẩn hóa từ khóa: xóa dấu tiếng Việt, ký tự đặc biệt, đưa về chữ thường và khoảng trắng đơn.
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalizedString = input.Trim().Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var withoutDiacritics = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        // Thay thế ký tự đ, Đ
        withoutDiacritics = withoutDiacritics.Replace('đ', 'd').Replace('Đ', 'D');

        // Giữ lại ký tự chữ, số và khoảng trắng
        var cleaned = new StringBuilder();
        foreach (var ch in withoutDiacritics)
        {
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            {
                cleaned.Append(ch);
            }
            else
            {
                cleaned.Append(' ');
            }
        }

        var result = MultipleSpacesRegex.Replace(cleaned.ToString(), " ").Trim().ToLowerInvariant();
        return result;
    }
}
