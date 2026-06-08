namespace EUROERP.Application.Validation;

/// <summary>Validação modular de CPF e CNPJ para reuso em toda a aplicação.</summary>
public static class CpfCnpjValidator
{
    private static ReadOnlySpan<int> CpfWeights1 => [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static ReadOnlySpan<int> CpfWeights2 => [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

    private static ReadOnlySpan<int> CnpjWeights1 => [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static ReadOnlySpan<int> CnpjWeights2 => [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>Extrai apenas dígitos da string.</summary>
    public static string OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Where(char.IsDigit).ToArray());
    }

    /// <summary>Retorna true se o valor for um CPF válido (11 dígitos e dígitos verificadores corretos). Aceita formatação (pontos e traço).</summary>
    public static bool IsValidCpf(string? value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length != 11) return false;
        if (AllSameDigit(digits)) return false;

        var span = digits.AsSpan();
        int sum1 = 0;
        for (int i = 0; i < 9; i++)
            sum1 += (span[i] - '0') * CpfWeights1[i];
        int d1 = (sum1 * 10) % 11;
        if (d1 == 10) d1 = 0;
        if (d1 != (span[9] - '0')) return false;

        int sum2 = 0;
        for (int i = 0; i < 10; i++)
            sum2 += (span[i] - '0') * CpfWeights2[i];
        int d2 = (sum2 * 10) % 11;
        if (d2 == 10) d2 = 0;
        if (d2 != (span[10] - '0')) return false;

        return true;
    }

    /// <summary>Retorna true se o valor for um CNPJ válido (14 dígitos e dígitos verificadores corretos). Aceita formatação.</summary>
    public static bool IsValidCnpj(string? value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length != 14) return false;
        if (AllSameDigit(digits)) return false;

        var span = digits.AsSpan();
        int sum1 = 0;
        for (int i = 0; i < 12; i++)
            sum1 += (span[i] - '0') * CnpjWeights1[i];
        int r1 = sum1 % 11;
        int d1 = r1 < 2 ? 0 : 11 - r1;
        if (d1 != (span[12] - '0')) return false;

        int sum2 = 0;
        for (int i = 0; i < 13; i++)
            sum2 += (span[i] - '0') * CnpjWeights2[i];
        int r2 = sum2 % 11;
        int d2 = r2 < 2 ? 0 : 11 - r2;
        if (d2 != (span[13] - '0')) return false;

        return true;
    }

    /// <summary>Retorna true se o valor for vazio/null ou um CPF ou CNPJ válido (útil para campos opcionais "CNPJ/CPF").</summary>
    public static bool IsValidCpfOrCnpj(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var digits = OnlyDigits(value);
        if (digits.Length == 11) return IsValidCpf(value);
        if (digits.Length == 14) return IsValidCnpj(value);
        return false;
    }

    /// <summary>Formata como CPF (XXX.XXX.XXX-XX). Retorna o valor original se não tiver 11 dígitos.</summary>
    public static string? FormatCpf(string? value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length != 11) return value;
        return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
    }

    /// <summary>Formata como CNPJ (XX.XXX.XXX/XXXX-XX). Retorna o valor original se não tiver 14 dígitos.</summary>
    public static string? FormatCnpj(string? value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length != 14) return value;
        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }

    /// <summary>Formata como CPF ou CNPJ conforme a quantidade de dígitos.</summary>
    public static string? FormatCpfOrCnpj(string? value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length == 11) return FormatCpf(value);
        if (digits.Length == 14) return FormatCnpj(value);
        return value;
    }

    private static bool AllSameDigit(string digits)
    {
        if (digits.Length == 0) return true;
        char first = digits[0];
        return digits.All(c => c == first);
    }
}
