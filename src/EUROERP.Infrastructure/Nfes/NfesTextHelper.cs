using System.Globalization;
using System.Text;

namespace EUROERP.Infrastructure.Nfes;

internal static class NfesTextHelper
{
    public static string LeftZero(string input, int length)
    {
        input = "000000000000000000000000000000000000000000000000000" + input;
        return input[^length..];
    }

    public static string RightSpaces(string input, int length)
    {
        input = input + "                                                                             ";
        return input[..length];
    }

    public static string Substring(string input, int len) =>
        input.Length > len ? input[..len] : input;

    public static string CleanDigits(string input)
    {
        var result = input.Replace(" - ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("(", "", StringComparison.Ordinal)
            .Replace(")", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("/", "", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal);
        return result.Trim();
    }

    public static string CleanStringToXml(string input, int len)
    {
        input = input.Replace("&", "", StringComparison.Ordinal);
        input = RemoveAccents(input);
        return Substring(input.Trim(), len);
    }

    public static string[] CutLines(string input, int size)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<string>();

        var lines = new List<string>();
        var remaining = input;
        while (remaining.Length > size)
        {
            lines.Add(remaining[..size]);
            remaining = remaining[size..];
        }
        if (remaining.Length > 0)
            lines.Add(remaining);
        return lines.ToArray();
    }

    private static string RemoveAccents(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
