namespace EUROERP.Infrastructure.Nfes;

internal static class NfesPrintUrlBuilder
{
  private const string ConsultaPublicaUrl = "https://www.nfse.gov.br/ConsultaPublica/?tpc=1&chave=";

  public static string? Build(
      NfesConfigSnapshot config,
      string? nfesNo,
      string? checkCode,
      string? pdfUrl = null,
      string? chaveAcesso = null)
  {
    if (config.UseSimpliss)
      return BuildSimpliss(pdfUrl, chaveAcesso ?? checkCode);

    return BuildPrefeituraSp(config.EmitIMun, nfesNo, checkCode);
  }

  private static string? BuildSimpliss(string? pdfUrl, string? chaveAcesso)
  {
    if (!string.IsNullOrWhiteSpace(pdfUrl) && pdfUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
      return pdfUrl.Trim();

    var chave = chaveAcesso?.Trim();
    if (string.IsNullOrWhiteSpace(chave) || !chave.StartsWith("NFS", StringComparison.OrdinalIgnoreCase))
      return null;

    return ConsultaPublicaUrl + Uri.EscapeDataString(chave);
  }

  private static string? BuildPrefeituraSp(string emitIm, string? nfesNo, string? checkCode)
  {
    if (string.IsNullOrWhiteSpace(nfesNo) || string.IsNullOrWhiteSpace(checkCode))
      return null;
    if (checkCode.StartsWith("NFS", StringComparison.OrdinalIgnoreCase))
      return null;

    var im = NfesTextHelper.CleanDigits(emitIm);
    if (string.IsNullOrEmpty(im))
      return null;

    return "https://nfe.prefeitura.sp.gov.br/contribuinte/notaprint.aspx"
        + $"?inscricao={im}&nf={Uri.EscapeDataString(nfesNo)}&verificacao={Uri.EscapeDataString(checkCode)}";
  }
}
