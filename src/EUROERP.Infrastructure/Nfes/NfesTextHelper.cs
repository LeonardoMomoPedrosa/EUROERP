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

    public static string FitDb(string? value, int maxLen) =>
        string.IsNullOrEmpty(value) ? "" : Substring(value.Trim(), maxLen);

    public static string CleanDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (c is >= '0' and <= '9')
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Brazil (UTC-3) emission instant, slightly in the past to satisfy E0008 (dhEmi &lt;= dhProc).</summary>
    public static DateTimeOffset GetBrazilEmissionInstant(int skewSeconds = 60) =>
        DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-3)).AddSeconds(-skewSeconds);

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
