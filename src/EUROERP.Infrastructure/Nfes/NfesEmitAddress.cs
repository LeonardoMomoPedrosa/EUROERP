namespace EUROERP.Infrastructure.Nfes;

internal static class NfesEmitAddress
{
    public static bool HasAddress(NfesConfigSnapshot config) =>
        !string.IsNullOrWhiteSpace(config.EmitLogradouro);

    public static string Format(NfesConfigSnapshot config)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(config.EmitLogradouro))
            parts.Add(config.EmitLogradouro.Trim());
        if (!string.IsNullOrWhiteSpace(config.EmitNumero))
            parts.Add(config.EmitNumero.Trim());
        if (!string.IsNullOrWhiteSpace(config.EmitComplemento))
            parts.Add(config.EmitComplemento.Trim());
        if (!string.IsNullOrWhiteSpace(config.EmitBairro))
            parts.Add(config.EmitBairro.Trim());

        var cityUf = string.Join("-",
            new[] { config.EmitMunicipio?.Trim(), config.EmitSiglaUf?.Trim() }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(cityUf))
            parts.Add(cityUf);

        var cep = NfesTextHelper.CleanDigits(config.EmitCep ?? "");
        if (cep.Length == 8)
            parts.Add("CEP " + $"{cep[..5]}-{cep[5..]}");

        return string.Join(", ", parts);
    }
}
