using System.Globalization;

namespace EUROERP.Infrastructure.Nfes;

/// <summary>Simpliss — layout nacional DPS (Santana de Parnaíba e demais municípios Simpliss).</summary>
public sealed class SimplissNfesBackend : INfesEmissionBackend
{
    private readonly INfesConfigProvider _configProvider;
    private readonly INfesCertificateProvider _certificateProvider;
    private readonly INfesSimplissClient _simplissClient;

    public SimplissNfesBackend(
        INfesConfigProvider configProvider,
        INfesCertificateProvider certificateProvider,
        INfesSimplissClient simplissClient)
    {
        _configProvider = configProvider;
        _certificateProvider = certificateProvider;
        _simplissClient = simplissClient;
    }

    public string ProviderKey => "Simpliss";

    public async Task<NfesEmissionOutcome> EmitAsync(NfesEmissionWorkItem work, CancellationToken cancellationToken)
    {
        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(config.EmitCnpj) || string.IsNullOrWhiteSpace(config.EmitIMun))
            return NfesEmissionOutcome.Fail("Configure CNPJ e Inscrição Municipal em Diretoria > Configuração NFES.");

        var dpsXml = NfesDpsBuilder.Build(work, config);
        var cert = _certificateProvider.GetCertificate();
        var signedDoc = NfesDpsXmlSupport.SignDps(dpsXml, cert);
        var signedXml = signedDoc.OuterXml;

        var orderFolder = Path.Combine(config.XmlPath, "S" + work.OrderId);
        Directory.CreateDirectory(orderFolder);
        var fileKey = work.OrderId.ToString(CultureInfo.InvariantCulture);
        var xmlPath = Path.Combine(orderFolder, fileKey + "-dps.xml");
        await File.WriteAllTextAsync(xmlPath, signedXml, cancellationToken).ConfigureAwait(false);

        var response = await _simplissClient.EmitDpsAsync(signedXml, cancellationToken).ConfigureAwait(false);
        if (!response.Success)
            return NfesEmissionOutcome.Fail(response.ErrorMessage ?? "Simpliss rejeitou a DPS.");

        if (!string.IsNullOrWhiteSpace(response.NfseXml))
            await File.WriteAllTextAsync(Path.Combine(orderFolder, fileKey + "-nfse.xml"), response.NfseXml, cancellationToken).ConfigureAwait(false);

        var (numero, codVerif) = !string.IsNullOrWhiteSpace(response.NfseXml)
            ? NfesNfseXmlParser.ParseAuthorizedNfse(response.NfseXml)
            : (null, null);

        var chaveAcesso = response.ChaveAcesso?.Trim();
        var nfesNo = numero;
        if (string.IsNullOrWhiteSpace(nfesNo) && !string.IsNullOrWhiteSpace(chaveAcesso) && chaveAcesso.Length >= 15)
            nfesNo = chaveAcesso[^15..].TrimStart('0');
        if (string.IsNullOrWhiteSpace(nfesNo))
            nfesNo = work.RpsNumber.ToString(CultureInfo.InvariantCulture);

        var printKey = !string.IsNullOrWhiteSpace(chaveAcesso) ? chaveAcesso : codVerif;
        var dbCheckCode = !string.IsNullOrWhiteSpace(codVerif) ? codVerif : chaveAcesso ?? "";

        return new NfesEmissionOutcome
        {
            Success = true,
            NfesNo = NfesTextHelper.FitDb(nfesNo, 15),
            RpsNo = work.RpsNumber.ToString(CultureInfo.InvariantCulture),
            CheckCode = NfesTextHelper.FitDb(dbCheckCode, 20),
            ChaveAcesso = printKey,
            PdfUrl = response.PdfUrl,
            XmlPath = xmlPath,
            Message = "NFS-e enviada com sucesso (Simpliss / layout nacional)."
        };
    }
}
