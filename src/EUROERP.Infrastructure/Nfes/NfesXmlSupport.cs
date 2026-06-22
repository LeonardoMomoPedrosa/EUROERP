using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Serialization;
using EUROERP.Infrastructure.Nfes.PrefeituraSp;

namespace EUROERP.Infrastructure.Nfes;

internal static class NfesXmlSupport
{
    public static XmlDocument SerializePedido(PedidoEnvioLoteRPS pedido)
    {
        var serializer = new XmlSerializer(typeof(PedidoEnvioLoteRPS));
        var doc = new XmlDocument { PreserveWhitespace = false };
        using var ms = new MemoryStream();
        serializer.Serialize(ms, pedido);
        ms.Position = 0;
        doc.Load(ms);
        return doc;
    }

    public static XmlDocument SerializePedidoCancelamento(PedidoCancelamentoNFe pedido)
    {
        var serializer = new XmlSerializer(typeof(PedidoCancelamentoNFe));
        var doc = new XmlDocument { PreserveWhitespace = false };
        using var ms = new MemoryStream();
        serializer.Serialize(ms, pedido);
        ms.Position = 0;
        doc.Load(ms);
        return doc;
    }

    public static T Deserialize<T>(string xml) where T : class
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new XmlNodeReader(doc.DocumentElement!);
        return (T)serializer.Deserialize(reader)!;
    }

    public static XmlDocument SignRpsDocument(XmlDocument doc, X509Certificate2 certificate)
    {
        FixXmlns(doc);
#pragma warning disable SYSLIB0028
        var key = certificate.GetRSAPrivateKey() ?? (System.Security.Cryptography.RSA?)certificate.PrivateKey;
#pragma warning restore SYSLIB0028
        if (key == null)
            throw new InvalidOperationException("Certificado sem chave privada.");

        var signedXml = new SignedXml(doc) { SigningKey = key };
        var reference = new Reference { Uri = "" };
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        signedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificate));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();

        var signatureElement = signedXml.GetXml();
        doc.DocumentElement?.AppendChild(doc.ImportNode(signatureElement, true));
        return doc;
    }

    public static void ValidatePedidoXml(string xsdPath, string xmlPath)
    {
        if (!File.Exists(xsdPath))
            return;

        var schemaSet = new System.Xml.Schema.XmlSchemaSet();
        schemaSet.Add(null, xsdPath);
        var doc = new XmlDocument();
        doc.Load(xmlPath);
        doc.Schemas = schemaSet;
        doc.Validate((_, e) =>
        {
            if (e.Severity == System.Xml.Schema.XmlSeverityType.Error)
                throw new InvalidOperationException("Validação XSD: " + e.Message);
        });
    }

    private static void FixXmlns(XmlDocument doc)
    {
        if (doc.DocumentElement == null)
            return;
        var toRemove = doc.DocumentElement.Attributes
            .Cast<XmlAttribute>()
            .Where(a => a.Name.StartsWith("xmlns:", StringComparison.Ordinal))
            .ToList();
        foreach (var attr in toRemove)
            doc.DocumentElement.Attributes.Remove(attr);
    }
}
