using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CulinaryBlog.Domain.Common;

public static class SlugHelper
{
    public static string Generate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // 1. Chuyển sang chữ thường
        var normalized = text.Trim().ToLowerInvariant();

        // 2. Xử lý riêng chữ đ/Đ
        normalized = normalized.Replace("đ", "d").Replace("Đ", "d");

        // 3. Tách dấu tiếng Việt bằng FormD và lọc bỏ NonSpacingMark
        var decomposed = normalized.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        normalized = sb.ToString().Normalize(NormalizationForm.FormC);

        // 4. Thay thế ký tự không phải chữ và số bằng gạch nối
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", "");

        // 5. Gộp nhiều khoảng trắng hoặc gạch nối thành 1 gạch nối
        normalized = Regex.Replace(normalized, @"[\s-]+", "-").Trim('-');

        return normalized;
    }
}
