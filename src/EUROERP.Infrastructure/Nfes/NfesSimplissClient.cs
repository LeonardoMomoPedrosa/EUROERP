using System.IO.Compression;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Microsoft.Extensions.Configuration;

namespace EUROERP.Infrastructure.Nfes;

internal static class NfesDpsXmlSupport
{
    public static XmlDocument SignDps(string dpsXml, X509Certificate2 certificate)
    {
        var doc = new XmlDocument { PreserveWhitespace = false };
        doc.LoadXml(dpsXml);
        var infDps = doc.GetElementsByTagName("infDPS").Cast<XmlElement>().FirstOrDefault()
            ?? throw new InvalidOperationException("DPS sem infDPS.");
        var id = infDps.GetAttribute("Id");
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("infDPS sem atributo Id.");

        return SignElementById(doc, id, certificate);
    }

    public static XmlDocument SignPedRegEvento(string pedRegXml, X509Certificate2 certificate)
    {
        var doc = new XmlDocument { PreserveWhitespace = false };
        doc.LoadXml(pedRegXml);
        var infPedReg = doc.GetElementsByTagName("infPedReg").Cast<XmlElement>().FirstOrDefault()
            ?? throw new InvalidOperationException("pedRegEvento sem infPedReg.");
        var id = infPedReg.GetAttribute("Id");
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("infPedReg sem atributo Id.");

        return SignElementById(doc, id, certificate);
    }

    private static XmlDocument SignElementById(XmlDocument doc, string id, X509Certificate2 certificate)
    {
#pragma warning disable SYSLIB0028
        var key = certificate.GetRSAPrivateKey() ?? (System.Security.Cryptography.RSA?)certificate.PrivateKey;
#pragma warning restore SYSLIB0028
        if (key == null)
            throw new InvalidOperationException("Certificado sem chave privada.");

        var signedXml = new SignedXml(doc) { SigningKey = key };
        var reference = new Reference { Uri = "#" + id };
        reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificate));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();

        var signatureElement = signedXml.GetXml();
        doc.DocumentElement?.AppendChild(doc.ImportNode(signatureElement, true));
        return doc;
    }

    public static string ToGzipBase64(string xml)
    {
        var bytes = Encoding.UTF8.GetBytes(xml);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(bytes, 0, bytes.Length);
        return Convert.ToBase64String(output.ToArray());
    }

    public static string FromGzipBase64(string base64)
    {
        var compressed = Convert.FromBase64String(base64);
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

public interface INfesSimplissClient
{
    Task<SimplissEmitResponse> EmitDpsAsync(string signedDpsXml, CancellationToken cancellationToken = default);
    Task<SimplissConsultResponse> GetNfseByChaveAsync(string chaveAcesso, CancellationToken cancellationToken = default);
    Task<SimplissCancelResponse> CancelNfseAsync(string chaveAcesso, string signedPedRegXml, CancellationToken cancellationToken = default);
}

public sealed class SimplissCancelResponse
{
    public bool Success { get; init; }
    public string? EventoXml { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class SimplissConsultResponse
{
    public bool Success { get; init; }
    public string? ChaveAcesso { get; init; }
    public string? NfseXml { get; init; }
    public string? PdfUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class SimplissEmitResponse
{
    public bool Success { get; init; }
    public string? ChaveAcesso { get; init; }
    public string? IdDps { get; init; }
    public string? NfseXml { get; init; }
    public string? PdfUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

public class NfesSimplissClient : INfesSimplissClient
{
    private readonly INfesConfigProvider _configProvider;
    private readonly INfesCertificateProvider _certificateProvider;

    public NfesSimplissClient(INfesConfigProvider configProvider, INfesCertificateProvider certificateProvider)
    {
        _configProvider = configProvider;
        _certificateProvider = certificateProvider;
    }

    public async Task<SimplissEmitResponse> EmitDpsAsync(string signedDpsXml, CancellationToken cancellationToken = default)
    {
        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var baseUrl = ResolveApiBaseUrl(config);
        var emitPath = string.IsNullOrWhiteSpace(config.ApiEmitPath) ? "/nfse" : config.ApiEmitPath;
        var url = baseUrl + emitPath;

        var payload = JsonSerializer.Serialize(new { dpsXmlGZipB64 = NfesDpsXmlSupport.ToGzipBase64(signedDpsXml) });
        using var client = CreateHttpClient(config);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return MapEmitResponse(response, responseText);
    }

    public async Task<SimplissConsultResponse> GetNfseByChaveAsync(string chaveAcesso, CancellationToken cancellationToken = default)
    {
        chaveAcesso = chaveAcesso?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(chaveAcesso))
            return new SimplissConsultResponse { Success = false, ErrorMessage = "Chave de acesso NFS-e não informada." };

        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var baseUrl = ResolveApiBaseUrl(config);
        var emitPath = string.IsNullOrWhiteSpace(config.ApiEmitPath) ? "/nfse" : config.ApiEmitPath.TrimEnd('/');
        var url = $"{baseUrl}{emitPath}/{Uri.EscapeDataString(chaveAcesso)}";

        using var client = CreateHttpClient(config);
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        SimplissApiResponse? body;
        try
        {
            body = JsonSerializer.Deserialize<SimplissApiResponse>(responseText, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new SimplissConsultResponse
            {
                Success = false,
                ErrorMessage = $"Resposta inválida ({(int)response.StatusCode}): {ex.Message}"
            };
        }

        if (body?.Erros is { Count: > 0 })
        {
            var msg = string.Join(Environment.NewLine, body.Erros.Select(e =>
                $"Erro {e.Codigo}: {e.Descricao} {e.Complemento}".Trim()));
            return new SimplissConsultResponse { Success = false, ErrorMessage = msg };
        }

        if (!response.IsSuccessStatusCode)
            return new SimplissConsultResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseText}"
            };

        string? xml = null;
        if (!string.IsNullOrWhiteSpace(body?.NfseXmlGZipB64))
        {
            try { xml = NfesDpsXmlSupport.FromGzipBase64(body.NfseXmlGZipB64); }
            catch (Exception ex)
            {
                return new SimplissConsultResponse { Success = false, ErrorMessage = $"XML NFS-e inválido: {ex.Message}" };
            }
        }

        if (string.IsNullOrWhiteSpace(xml))
            return new SimplissConsultResponse { Success = false, ErrorMessage = "Webservice não retornou XML da NFS-e." };

        return new SimplissConsultResponse
        {
            Success = true,
            ChaveAcesso = body?.ChaveAcesso?.Trim() ?? chaveAcesso,
            NfseXml = xml,
            PdfUrl = body?.UrlPdf
        };
    }

    public async Task<SimplissCancelResponse> CancelNfseAsync(string chaveAcesso, string signedPedRegXml, CancellationToken cancellationToken = default)
    {
        chaveAcesso = NfesTextHelper.CleanDigits(chaveAcesso?.Trim() ?? "");
        if (chaveAcesso.Length > 50)
            chaveAcesso = chaveAcesso[^50..];
        if (chaveAcesso.Length < 50)
            return new SimplissCancelResponse { Success = false, ErrorMessage = "Chave de acesso NFS-e inválida." };

        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var baseUrl = ResolveApiBaseUrl(config);
        var emitPath = string.IsNullOrWhiteSpace(config.ApiEmitPath) ? "/nfse" : config.ApiEmitPath.TrimEnd('/');
        var url = $"{baseUrl}{emitPath}/{Uri.EscapeDataString(chaveAcesso)}/eventos";

        var payload = JsonSerializer.Serialize(new { pedidoRegistroEventoXmlGZipB64 = NfesDpsXmlSupport.ToGzipBase64(signedPedRegXml) });
        using var client = CreateHttpClient(config);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return MapCancelResponse(response, responseText);
    }

    private static SimplissCancelResponse MapCancelResponse(HttpResponseMessage response, string responseText)
    {
        SimplissApiResponse? body;
        try
        {
            body = JsonSerializer.Deserialize<SimplissApiResponse>(responseText, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new SimplissCancelResponse
            {
                Success = false,
                ErrorMessage = $"Resposta inválida ({(int)response.StatusCode}): {ex.Message}"
            };
        }

        if (body?.Erros is { Count: > 0 })
        {
            var msg = string.Join(Environment.NewLine, body.Erros.Select(e =>
                $"Erro {e.Codigo}: {e.Descricao} {e.Complemento}".Trim()));
            return new SimplissCancelResponse { Success = false, ErrorMessage = msg };
        }

        if (!response.IsSuccessStatusCode)
            return new SimplissCancelResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseText}"
            };

        string? eventoXml = null;
        var gzipField = body?.EventoXmlGZipB64 ?? body?.NfseXmlGZipB64;
        if (!string.IsNullOrWhiteSpace(gzipField))
        {
            try { eventoXml = NfesDpsXmlSupport.FromGzipBase64(gzipField); }
            catch (Exception ex)
            {
                return new SimplissCancelResponse { Success = false, ErrorMessage = $"XML do evento inválido: {ex.Message}" };
            }
        }

        return new SimplissCancelResponse { Success = true, EventoXml = eventoXml };
    }

    private static string ResolveApiBaseUrl(NfesConfigSnapshot config) =>
        (config.IsTestEnvironment ? config.ApiBaseUrlHomolog : config.ApiBaseUrl).TrimEnd('/');

    private HttpClient CreateHttpClient(NfesConfigSnapshot config)
    {
        var cert = _certificateProvider.GetCertificate();
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return new HttpClient(handler);
    }

    private static SimplissEmitResponse MapEmitResponse(HttpResponseMessage response, string responseText)
    {
        SimplissApiResponse? body;
        try
        {
            body = JsonSerializer.Deserialize<SimplissApiResponse>(responseText, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new SimplissEmitResponse
            {
                Success = false,
                ErrorMessage = $"Resposta inválida ({(int)response.StatusCode}): {ex.Message}"
            };
        }

        if (body == null)
            return new SimplissEmitResponse
            {
                Success = false,
                ErrorMessage = $"Resposta vazia ({(int)response.StatusCode})."
            };

        if (body.Erros is { Count: > 0 })
        {
            var msg = string.Join(Environment.NewLine, body.Erros.Select(e =>
                $"Erro {e.Codigo}: {e.Descricao} {e.Complemento}".Trim()));
            return new SimplissEmitResponse { Success = false, ErrorMessage = msg };
        }

        if (!response.IsSuccessStatusCode)
            return new SimplissEmitResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseText}"
            };

        var nfseXml = body.NfseXmlGZipB64;
        string? xml = null;
        if (!string.IsNullOrWhiteSpace(nfseXml))
        {
            try { xml = NfesDpsXmlSupport.FromGzipBase64(nfseXml); }
            catch { /* parsed below */ }
        }

        return new SimplissEmitResponse
        {
            Success = true,
            ChaveAcesso = body.ChaveAcesso,
            IdDps = body.IdDps,
            NfseXml = xml,
            PdfUrl = body.UrlPdf
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class SimplissApiResponse
    {
        [JsonPropertyName("chaveAcesso")]
        public string? ChaveAcesso { get; set; }

        [JsonPropertyName("idDps")]
        public string? IdDps { get; set; }

        [JsonPropertyName("nfseXmlGZipB64")]
        public string? NfseXmlGZipB64 { get; set; }

        [JsonPropertyName("eventoXmlGZipB64")]
        public string? EventoXmlGZipB64 { get; set; }

        [JsonPropertyName("urlPdf")]
        public string? UrlPdf { get; set; }

        [JsonPropertyName("erros")]
        public List<SimplissApiError>? Erros { get; set; }
    }

    private sealed class SimplissApiError
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }

        [JsonPropertyName("descricao")]
        public string? Descricao { get; set; }

        [JsonPropertyName("complemento")]
        public string? Complemento { get; set; }
    }
}

internal static class NfesNfseXmlParser
{
    public static string? ParseChaveAcesso(string nfseXml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(nfseXml);
        var id = doc.GetElementsByTagName("infNFSe").Cast<XmlElement>().FirstOrDefault()?.GetAttribute("Id");
        return string.IsNullOrWhiteSpace(id) ? null : id.Trim();
    }

    public static (string? Numero, string? CodigoVerificacao) ParseAuthorizedNfse(string nfseXml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(nfseXml);
        var nNfse = doc.GetElementsByTagName("nNFSe").Cast<XmlNode>().FirstOrDefault()?.InnerText?.Trim();
        if (string.IsNullOrWhiteSpace(nNfse))
            nNfse = doc.GetElementsByTagName("nDFSe").Cast<XmlNode>().FirstOrDefault()?.InnerText?.Trim();

        var cVerif = doc.GetElementsByTagName("cVerif").Cast<XmlNode>().FirstOrDefault()?.InnerText?.Trim();
        if (string.IsNullOrWhiteSpace(cVerif))
            cVerif = doc.GetElementsByTagName("CodigoVerificacao").Cast<XmlNode>().FirstOrDefault()?.InnerText?.Trim();

        return (nNfse, cVerif);
    }
}
