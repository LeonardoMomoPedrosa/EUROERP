using System.Security.Cryptography.Xml;
using System.Xml;

namespace EUROERP.Infrastructure.NFe;

public interface INfeXmlSigner
{
    XmlDocument SignNfeXml(XmlDocument nfeDoc, string infNfeIdAttributeValue);

    /// <summary>Sign event XML (e.g. envEvento). Signs the element whose Id attribute equals the given value (e.g. ID110111...).</summary>
    XmlDocument SignEventXml(XmlDocument eventDoc, string elementIdAttributeValue);
}

public class NfeXmlSigner : INfeXmlSigner
{
    private readonly INfeCertificateProvider _certProvider;

    public NfeXmlSigner(INfeCertificateProvider certProvider)
    {
        _certProvider = certProvider;
    }

    public XmlDocument SignNfeXml(XmlDocument nfeDoc, string infNfeIdAttributeValue)
    {
        var cert = _certProvider.GetCertificate();
        #pragma warning disable SYSLIB0028
        var key = (System.Security.Cryptography.RSA?)cert.PrivateKey;
#pragma warning restore SYSLIB0028
        if (key == null)
            throw new InvalidOperationException("Certificado não possui chave privada.");

        FixXmlns(nfeDoc);

        var signedXml = new SignedXml(nfeDoc);
        signedXml.SigningKey = key;

        var reference = new Reference
        {
            Uri = "#" + infNfeIdAttributeValue
        };
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url; // http://www.w3.org/2000/09/xmldsig#sha1 (schema fixed)
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url; // rsa-sha1 (schema fixed)
        signedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315"; // schema fixed

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();
        var signatureElement = signedXml.GetXml();
        nfeDoc.DocumentElement?.AppendChild(nfeDoc.ImportNode(signatureElement, true));

        return nfeDoc;
    }

    /// <summary>
    /// Assina o evento (envEvento). A assinatura deve ficar dentro do elemento "evento" (pai de infEvento),
    /// conforme schema leiauteEventoCancNFe: evento = infEvento + Signature.
    /// Igual ao legado: GetElementsByTagName(aTag)[0].ParentNode.AppendChild(signature).
    /// </summary>
    public XmlDocument SignEventXml(XmlDocument eventDoc, string elementIdAttributeValue)
    {
        var cert = _certProvider.GetCertificate();
#pragma warning disable SYSLIB0028
        var key = (System.Security.Cryptography.RSA?)cert.PrivateKey;
#pragma warning restore SYSLIB0028
        if (key == null)
            throw new InvalidOperationException("Certificado não possui chave privada.");

        FixXmlns(eventDoc);

        // Legado: adiciona xmlns no elemento "evento" (ChildNodes[1] = envEvento, ChildNodes[1].ChildNodes[1] = primeiro evento)
        var eventoElement = eventDoc.DocumentElement?.SelectSingleNode("*[local-name()='evento']") as XmlElement;
        if (eventoElement != null)
        {
            eventoElement.SetAttribute("xmlns", "http://www.portalfiscal.inf.br/nfe");
        }

        var signedXml = new SignedXml(eventDoc);
        signedXml.SigningKey = key;

        var reference = new Reference { Uri = "#" + elementIdAttributeValue };
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        signedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();
        var signatureElement = signedXml.GetXml();

        // Assinatura deve ser filha de "evento", não de "envEvento" (conforme XSD: evento = infEvento + Signature).
        // Legado: GetElementsByTagName(aTag)[nodeList.Count - 1].ParentNode.AppendChild(signature)
        var infEventoList = eventDoc.GetElementsByTagName("infEvento");
        var infEventoNode = infEventoList.Count > 0 ? infEventoList[infEventoList.Count - 1] : null;
        var parentEvento = infEventoNode?.ParentNode;
        if (parentEvento != null)
            parentEvento.AppendChild(eventDoc.ImportNode(signatureElement, true));
        else
            eventDoc.DocumentElement?.AppendChild(eventDoc.ImportNode(signatureElement, true));

        return eventDoc;
    }

    /// <summary>
    /// Remove atributos xmlns:* do elemento raiz para evitar conflito na assinatura (portado do legado fixXmlns).
    /// </summary>
    private static void FixXmlns(XmlDocument doc)
    {
        if (doc.ChildNodes.Count < 2) return;
        var root = doc.ChildNodes[1] as XmlElement;
        if (root == null) return;

        var toRemove = new List<XmlAttribute>();
        foreach (XmlAttribute attr in root.Attributes!)
        {
            if (attr.Name.StartsWith("xmlns:", StringComparison.Ordinal))
                toRemove.Add(attr);
        }
        foreach (var a in toRemove)
            root.Attributes.Remove(a);
    }
}
