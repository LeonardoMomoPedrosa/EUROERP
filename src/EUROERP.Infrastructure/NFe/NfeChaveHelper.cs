namespace EUROERP.Infrastructure.NFe;

/// <summary>
/// Gera chave de acesso NFe 44 dígitos e dígito verificador (módulo 11).
/// </summary>
public static class NfeChaveHelper
{
    /// <summary>
    /// Gera chave de acesso: UF (2) + AAMM (4) + CNPJ (14) + mod (2) + serie (3) + nNF (9) + tpEmis (1) + cNF (8) + DV (1) = 44.
    /// </summary>
    /// <param name="cnpj">CNPJ apenas dígitos (14).</param>
    /// <param name="date">Data de emissão.</param>
    /// <param name="nNF">Número da NF (1-999999999).</param>
    /// <param name="cNF">Código numérico 8 dígitos (ex: orderId zero-padded).</param>
    /// <param name="tpEmis">1=Normal, 2=Contingência, etc.</param>
    public static string GetChaveAcesso(string cnpj, DateTime date, int nNF, int cNF, int tpEmis = 1)
    {
        var cnpjClean = CleanDigits(cnpj);
        if (cnpjClean.Length != 14)
            throw new ArgumentException("CNPJ deve ter 14 dígitos.", nameof(cnpj));

        var sb = new System.Text.StringBuilder();
        sb.Append("35"); // UF SP
        sb.Append(date.ToString("yyMM"));
        sb.Append(cnpjClean);
        sb.Append("55");   // modelo 55
        sb.Append("000");  // série
        sb.Append(LeftZero(nNF.ToString(), 9));
        sb.Append(tpEmis.ToString());
        sb.Append(LeftZero(cNF.ToString(), 8));
        int dv = Mod11(sb.ToString());
        sb.Append(dv);
        return sb.ToString();
    }

    public static string LeftZero(string value, int length)
    {
        value = value.Trim();
        if (value.Length >= length) return value.Length > length ? value.Substring(value.Length - length, length) : value;
        return value.PadLeft(length, '0');
    }

    public static string CleanDigits(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return new string(input.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Remove máscara da inscrição estadual (pontos, traços, espaços, etc.), alinhado ao legado <c>NFEUtils.cleanString</c> usado em IE.
    /// </summary>
    public static string CleanIe(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var a = input.Replace(" - ", "", StringComparison.Ordinal);
        a = a.Replace("-", "", StringComparison.Ordinal);
        a = a.Replace("(", "", StringComparison.Ordinal);
        a = a.Replace(")", "", StringComparison.Ordinal);
        a = a.Replace(" ", "", StringComparison.Ordinal);
        a = a.Replace("/", "", StringComparison.Ordinal);
        a = a.Replace(".", "", StringComparison.Ordinal);
        return a.Trim();
    }

    private static int Mod11(string text)
    {
        int[] pesos = { 2, 3, 4, 5, 6, 7, 8, 9, 2, 3, 4, 5, 6, 7, 8, 9 };
        int soma = 0, idx = 0;
        for (int pos = text.Length - 1; pos >= 0; pos--)
        {
            soma += (text[pos] - '0') * pesos[idx];
            idx = idx == 9 ? 2 : idx + 1;
        }
        int resto = (soma * 10) % 11;
        return resto >= 10 ? 0 : resto;
    }
}
