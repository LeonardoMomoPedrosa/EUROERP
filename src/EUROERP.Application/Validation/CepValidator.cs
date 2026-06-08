namespace EUROERP.Application.Validation;

/// <summary>Validação modular de CEP (Código de Endereçamento Postal) para reuso em toda a aplicação.</summary>
public static class CepValidator
{
    /// <summary>Retorna true se o valor for um CEP válido (8 dígitos). Aceita formatação (traço). Vazio retorna true (campo opcional).</summary>
    public static bool IsValidCep(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var digits = CpfCnpjValidator.OnlyDigits(value);
        return digits.Length == 8;
    }

    /// <summary>Formata como CEP (00000-000). Retorna o valor original se não tiver 8 dígitos.</summary>
    public static string? FormatCep(string? value)
    {
        var digits = CpfCnpjValidator.OnlyDigits(value);
        if (digits.Length != 8) return value;
        return $"{digits[..5]}-{digits[5..]}";
    }
}
