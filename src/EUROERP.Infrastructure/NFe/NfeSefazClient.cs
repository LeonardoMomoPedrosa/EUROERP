using System.Net.Http.Headers;
using System.Text;
using System.Xml;

namespace EUROERP.Infrastructure.NFe;

/// <summary>
/// Cliente SOAP para NFe (Autorizacao e Recepção de Evento).
/// </summary>
public interface INfeSefazClient
{
    /// <summary>
    /// Envia lote de NFe para SEFAZ (modo síncrono). Retorna XML do nfeResultMsg para parsing (TRetEnviNFe).
    /// </summary>
    Task<XmlDocument> NfeAutorizacaoLoteAsync(string enviNfeXml, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia evento (ex.: cancelamento) para SEFAZ. Retorna XML retEnvEvento para parsing.
    /// </summary>
    Task<XmlDocument> NfeRecepcaoEventoAsync(string envEventoXml, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta status do serviço NFe na SEFAZ. Retorna XML retConsStatServ para parsing.
    /// </summary>
    Task<XmlDocument> NfeStatusServicoAsync(string consStatServXml, CancellationToken cancellationToken = default);
}

public class NfeSefazClient : INfeSefazClient
{
    private readonly INfeCertificateProvider _certProvider;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    /// <summary>User-Agent enviado nas requisições à SEFAZ para evitar bloqueio 403 por WAF/firewall (IIS). WAFs costumam aceitar apenas User-Agents de navegador conhecidos.</summary>
    private const string SefazUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public NfeSefazClient(
        INfeCertificateProvider certProvider,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _certProvider = certProvider;
        _configuration = configuration;
    }

    private static void SetSefazRequestHeaders(HttpRequestMessage request)
    {
        request.Headers.UserAgent.ParseAdd(SefazUserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/soap+xml"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml", 0.9));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("pt-BR"));
    }

    public async Task<XmlDocument> NfeAutorizacaoLoteAsync(string enviNfeXml, CancellationToken cancellationToken = default)
    {
        var url = _configuration["NFe:NfeAutorizacao"];
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("NFe:NfeAutorizacao não configurado.");

        var cert = _certProvider.GetCertificate();
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };

        // SOAP 1.2 envelope: nfeDadosMsg contém o XML do enviNFe (NFe + idLote + versao + indSinc)
        var soapEnvelope = BuildSoapEnvelope(enviNfeXml);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "application/soap+xml");
        request.Headers.Add("SOAPAction", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4/nfeAutorizacaoLote");
        SetSefazRequestHeaders(request);

        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        XmlDocument doc;
        try
        {
            doc = new XmlDocument();
            doc.LoadXml(responseBody);
        }
        catch (XmlException)
        {
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"SEFAZ retornou HTTP {(int)response.StatusCode}. Resposta: {TruncateForMessage(responseBody, 500)}");
            throw;
        }

        // Se a SEFAZ retornou erro HTTP (ex.: 500), tentar extrair SOAP Fault ou retEnviNFe do body para mostrar na tela
        if (!response.IsSuccessStatusCode)
        {
            var faultOrRetMsg = TryGetFaultOrRetEnviNFeMessage(doc);
            throw new InvalidOperationException(string.IsNullOrEmpty(faultOrRetMsg)
                ? $"SEFAZ retornou HTTP {(int)response.StatusCode}. Resposta: {TruncateForMessage(responseBody, 300)}"
                : $"SEFAZ HTTP {(int)response.StatusCode}: {faultOrRetMsg}");
        }

        // Extrair nfeResultMsg do body
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
        nsMgr.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4");
        var resultNode = doc.SelectSingleNode("//nfe:nfeResultMsg", nsMgr) ?? doc.SelectSingleNode("//*[local-name()='nfeResultMsg']");
        if (resultNode == null)
            throw new InvalidOperationException("Resposta SEFAZ não contém nfeResultMsg.");

        // Igual ao legado: primeiro elemento filho é retEnviNFe (deserializado como TRetEnviNFe)
        var firstChild = resultNode.FirstChild;
        while (firstChild != null && firstChild.NodeType != XmlNodeType.Element)
            firstChild = firstChild.NextSibling;
        if (firstChild == null)
            throw new InvalidOperationException("Resposta SEFAZ: nfeResultMsg vazio.");

        var resultDoc = new XmlDocument();
        resultDoc.LoadXml(firstChild.OuterXml);
        return resultDoc;
    }

    public async Task<XmlDocument> NfeRecepcaoEventoAsync(string envEventoXml, CancellationToken cancellationToken = default)
    {
        var url = _configuration["NFe:NfeRecepcaoEvento"];
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("NFe:NfeRecepcaoEvento não configurado.");

        var cert = _certProvider.GetCertificate();
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };

        var soapEnvelope = BuildRecepcaoEventoSoapEnvelope(envEventoXml);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "application/soap+xml");
        request.Headers.Add("SOAPAction", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4/nfeRecepcaoEvento");
        SetSefazRequestHeaders(request);

        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        XmlDocument doc;
        try
        {
            doc = new XmlDocument();
            doc.LoadXml(responseBody);
        }
        catch (XmlException)
        {
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"SEFAZ retornou HTTP {(int)response.StatusCode}. Resposta: {TruncateForMessage(responseBody, 500)}");
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            var faultOrRetMsg = TryGetFaultOrRetEnvEventoMessage(doc);
            throw new InvalidOperationException(string.IsNullOrEmpty(faultOrRetMsg)
                ? $"SEFAZ retornou HTTP {(int)response.StatusCode}. Resposta: {TruncateForMessage(responseBody, 300)}"
                : $"SEFAZ HTTP {(int)response.StatusCode}: {faultOrRetMsg}");
        }

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4");
        var resultNode = doc.SelectSingleNode("//nfe:nfeResultMsg", nsMgr) ?? doc.SelectSingleNode("//*[local-name()='nfeResultMsg']");
        if (resultNode == null)
            throw new InvalidOperationException("Resposta SEFAZ não contém nfeResultMsg.");

        var firstChild = resultNode.FirstChild;
        while (firstChild != null && firstChild.NodeType != XmlNodeType.Element)
            firstChild = firstChild.NextSibling;
        if (firstChild == null)
            throw new InvalidOperationException("Resposta SEFAZ: nfeResultMsg vazio.");

        var resultDoc = new XmlDocument();
        resultDoc.LoadXml(firstChild.OuterXml);
        return resultDoc;
    }

    public async Task<XmlDocument> NfeStatusServicoAsync(string consStatServXml, CancellationToken cancellationToken = default)
    {
        var url = _configuration["NFe:NfeStatusServico"];
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("NFe:NfeStatusServico não configurado.");

        var cert = _certProvider.GetCertificate();
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };

        var soapEnvelope = BuildStatusServicoSoapEnvelope(consStatServXml);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "application/soap+xml");
        request.Headers.Add("SOAPAction", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeStatusServico4/nfeStatusServicoNF");
        SetSefazRequestHeaders(request);

        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        XmlDocument doc;
        try
        {
            doc = new XmlDocument();
            doc.LoadXml(responseBody);
        }
        catch (XmlException)
        {
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"SEFAZ retornou HTTP {(int)response.StatusCode}. Resposta: {TruncateForMessage(responseBody, 500)}");
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            var faultOrRetMsg = TryGetFaultOrRetConsStatServMessage(doc);
            throw new InvalidOperationException(string.IsNullOrEmpty(faultOrRetMsg)
                ? $"SEFAZ retornou HTTP {(int)response.StatusCode}. Resposta: {TruncateForMessage(responseBody, 300)}"
                : $"SEFAZ HTTP {(int)response.StatusCode}: {faultOrRetMsg}");
        }

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeStatusServico4");
        var resultNode = doc.SelectSingleNode("//nfe:nfeResultMsg", nsMgr) ?? doc.SelectSingleNode("//*[local-name()='nfeResultMsg']");
        if (resultNode == null)
            throw new InvalidOperationException("Resposta SEFAZ não contém nfeResultMsg.");

        var firstChild = resultNode.FirstChild;
        while (firstChild != null && firstChild.NodeType != XmlNodeType.Element)
            firstChild = firstChild.NextSibling;
        if (firstChild == null)
            throw new InvalidOperationException("Resposta SEFAZ: nfeResultMsg vazio.");

        var resultDoc = new XmlDocument();
        resultDoc.LoadXml(firstChild.OuterXml);
        return resultDoc;
    }

    private static string? TryGetFaultOrRetConsStatServMessage(XmlDocument doc)
    {
        var reason = doc.SelectSingleNode("//*[local-name()='Reason']") ?? doc.SelectSingleNode("//*[local-name()='Fault']/*[local-name()='Reason']");
        if (reason != null)
        {
            var text = reason.SelectSingleNode(".//*[local-name()='Text']");
            var msg = text?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(msg)) return msg;
        }
        var faultString = doc.SelectSingleNode("//*[local-name()='faultstring']");
        if (faultString != null && !string.IsNullOrWhiteSpace(faultString.InnerText))
            return faultString.InnerText.Trim();
        var ret = doc.SelectSingleNode("//*[local-name()='retConsStatServ']");
        if (ret != null)
        {
            var cStat = ret.SelectSingleNode("*[local-name()='cStat']")?.InnerText?.Trim();
            var xMotivo = ret.SelectSingleNode("*[local-name()='xMotivo']")?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(cStat) || !string.IsNullOrEmpty(xMotivo))
                return $"Erro {cStat ?? "?"} - {xMotivo ?? ""}".Trim();
        }
        return null;
    }

    private static string BuildStatusServicoSoapEnvelope(string consStatServXml)
    {
        var inner = consStatServXml.Trim();
        if (inner.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var end = inner.IndexOf("?>", StringComparison.Ordinal);
            if (end >= 0) inner = inner.Substring(end + 2).Trim();
        }
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:nfe=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeStatusServico4"">
  <soap:Header/>
  <soap:Body>
    <nfe:nfeDadosMsg>" + inner + @"</nfe:nfeDadosMsg>
  </soap:Body>
</soap:Envelope>";
    }

    private static string BuildRecepcaoEventoSoapEnvelope(string envEventoXml)
    {
        var inner = envEventoXml.Trim();
        if (inner.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var end = inner.IndexOf("?>", StringComparison.Ordinal);
            if (end >= 0) inner = inner.Substring(end + 2).Trim();
        }
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:nfe=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4"">
  <soap:Header/>
  <soap:Body>
    <nfe:nfeDadosMsg>" + inner + @"</nfe:nfeDadosMsg>
  </soap:Body>
</soap:Envelope>";
    }

    private static string? TryGetFaultOrRetEnvEventoMessage(XmlDocument doc)
    {
        var reason = doc.SelectSingleNode("//*[local-name()='Reason']") ?? doc.SelectSingleNode("//*[local-name()='Fault']/*[local-name()='Reason']");
        if (reason != null)
        {
            var text = reason.SelectSingleNode(".//*[local-name()='Text']");
            var msg = text?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(msg)) return msg;
        }
        var faultString = doc.SelectSingleNode("//*[local-name()='faultstring']");
        if (faultString != null && !string.IsNullOrWhiteSpace(faultString.InnerText))
            return faultString.InnerText.Trim();
        var retEnvEvento = doc.SelectSingleNode("//*[local-name()='retEnvEvento']");
        if (retEnvEvento != null)
        {
            var cStat = retEnvEvento.SelectSingleNode("*[local-name()='cStat']")?.InnerText?.Trim();
            var xMotivo = retEnvEvento.SelectSingleNode("*[local-name()='xMotivo']")?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(cStat) || !string.IsNullOrEmpty(xMotivo))
                return $"Erro {cStat ?? "?"} - {xMotivo ?? ""}".Trim();
        }
        return null;
    }

    /// <summary>
    /// Tenta extrair mensagem de erro do SOAP Fault ou de retEnviNFe (cStat/xMotivo) para exibir ao usuário.
    /// </summary>
    private static string? TryGetFaultOrRetEnviNFeMessage(XmlDocument doc)
    {
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
        nsMgr.AddNamespace("soap12", "http://www.w3.org/2003/05/soap-envelope");

        // SOAP 1.2 Fault: Reason/Text ou faultstring
        var reason = doc.SelectSingleNode("//*[local-name()='Reason']") ?? doc.SelectSingleNode("//*[local-name()='Fault']/*[local-name()='Reason']");
        if (reason != null)
        {
            var text = reason.SelectSingleNode(".//*[local-name()='Text']");
            var msg = text?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(msg)) return msg;
        }
        var faultString = doc.SelectSingleNode("//*[local-name()='faultstring']");
        if (faultString != null && !string.IsNullOrWhiteSpace(faultString.InnerText))
            return faultString.InnerText.Trim();

        // retEnviNFe dentro do body (cStat + xMotivo)
        var retEnviNFe = doc.SelectSingleNode("//*[local-name()='retEnviNFe']");
        if (retEnviNFe != null)
        {
            var cStat = retEnviNFe.SelectSingleNode("*[local-name()='cStat']")?.InnerText?.Trim();
            var xMotivo = retEnviNFe.SelectSingleNode("*[local-name()='xMotivo']")?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(cStat) || !string.IsNullOrEmpty(xMotivo))
                return $"Erro {cStat ?? "?"} - {xMotivo ?? ""}".Trim();
        }
        return null;
    }

    private static string TruncateForMessage(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Trim();
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength) + "...";
    }

    private static string BuildSoapEnvelope(string enviNfeXml)
    {
        // Remover declaração XML e BOM do enviNfe se existir, para encapsular no SOAP
        var inner = enviNfeXml.Trim();
        if (inner.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var end = inner.IndexOf("?>", StringComparison.Ordinal);
            if (end >= 0) inner = inner.Substring(end + 2).Trim();
        }

        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:nfe=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4"">
  <soap:Header/>
  <soap:Body>
    <nfe:nfeDadosMsg>" + inner + @"</nfe:nfeDadosMsg>
  </soap:Body>
</soap:Envelope>";
    }
}
