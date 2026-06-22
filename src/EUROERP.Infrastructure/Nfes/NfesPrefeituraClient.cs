using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace EUROERP.Infrastructure.Nfes;

public interface INfesPrefeituraClient
{
    Task<string> SendLoteRpsAsync(string signedXml, CancellationToken cancellationToken = default);
    Task<string> SendCancelamentoAsync(string signedXml, CancellationToken cancellationToken = default);
}

public class NfesPrefeituraClient : INfesPrefeituraClient
{
    private const string Ns = "http://www.prefeitura.sp.gov.br/nfe";
    private readonly INfesConfigProvider _configProvider;
    private readonly INfesCertificateProvider _certificateProvider;

    public NfesPrefeituraClient(INfesConfigProvider configProvider, INfesCertificateProvider certificateProvider)
    {
        _configProvider = configProvider;
        _certificateProvider = certificateProvider;
    }

    public async Task<string> SendLoteRpsAsync(string signedXml, CancellationToken cancellationToken = default)
    {
        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var isTest = config.IsTestEnvironment;
        var url = string.IsNullOrWhiteSpace(config.LoteNfeUrl)
            ? "https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx"
            : config.LoteNfeUrl;
        var action = isTest
            ? $"{Ns}/TesteEnvioLoteRPS"
            : $"{Ns}/EnvioLoteRPS";
        var requestElement = isTest ? "TesteEnvioLoteRPS" : "EnvioLoteRPS";

        return await PostSoapAsync(url, action, requestElement, signedXml, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> SendCancelamentoAsync(string signedXml, CancellationToken cancellationToken = default)
    {
        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var isTest = config.IsTestEnvironment;
        var url = string.IsNullOrWhiteSpace(config.LoteNfeUrl)
            ? "https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx"
            : config.LoteNfeUrl;
        var action = isTest
            ? $"{Ns}/TesteCancelamentoNFe"
            : $"{Ns}/CancelamentoNFe";
        var requestElement = isTest ? "TesteCancelamentoNFe" : "CancelamentoNFe";

        return await PostSoapAsync(url, action, requestElement, signedXml, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> PostSoapAsync(string url, string action, string requestElement, string signedXml, CancellationToken cancellationToken)
    {
        var escapedXml = System.Security.SecurityElement.Escape(signedXml) ?? signedXml;
        var envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <{requestElement} xmlns="{Ns}">
                  <VersaoSchema>1</VersaoSchema>
                  <MensagemXML>{escapedXml}</MensagemXML>
                </{requestElement}>
              </soap:Body>
            </soap:Envelope>
            """;

        var cert = _certificateProvider.GetCertificate();
        using var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
        content.Headers.Add("SOAPAction", action);

        using var response = await client.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Prefeitura SP HTTP {(int)response.StatusCode}: {responseText}");

        var doc = XDocument.Parse(responseText);
        XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        var body = doc.Root?.Element(soapNs + "Body");
        var retorno = body?.Descendants().FirstOrDefault(e => e.Name.LocalName == "RetornoXML");
        if (retorno != null && !string.IsNullOrWhiteSpace(retorno.Value))
            return retorno.Value.Trim();
        if (retorno != null && retorno.HasElements)
            return retorno.ToString(SaveOptions.DisableFormatting);

        return responseText;
    }
}
