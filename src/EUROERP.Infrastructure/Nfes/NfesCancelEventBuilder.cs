using System.Globalization;
using System.Text;
using System.Xml;

namespace EUROERP.Infrastructure.Nfes;

/// <summary>Pedido de registro de evento e101101 (cancelamento NFS-e nacional / Simpliss).</summary>
internal static class NfesCancelEventBuilder
{
    private const string Ns = "http://www.sped.fazenda.gov.br/nfse";
    private const string CancelEventCode = "101101";

    public static string Build(NfesConfigSnapshot config, string chaveAcesso, string xMotivo, string motivoCode)
    {
        var emitCnpj = NfesTextHelper.CleanDigits(config.EmitCnpj);
        var chave = NfesTextHelper.CleanDigits(chaveAcesso);
        if (chave.Length < 50)
            throw new InvalidOperationException("Chave de acesso NFS-e inválida para cancelamento.");
        if (chave.Length > 50)
            chave = chave[^50..];

        var motivo = (motivoCode ?? "9").Trim();
        if (motivo is not ("1" or "2" or "9"))
            motivo = "9";

        var descricao = NfesTextHelper.CleanStringToXml(xMotivo.Trim(), 255);
        if (descricao.Length < 15)
            throw new InvalidOperationException("O motivo deve ter no mínimo 15 caracteres.");

        var tpAmb = config.IsTestEnvironment ? "2" : "1";
        var idPedReg = $"PRE{chave}{CancelEventCode}";
        var dhEvento = NfesTextHelper.GetBrazilEmissionInstant(5)
            .ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        var verAplic = string.IsNullOrWhiteSpace(config.VersaoAplicativo) ? "EUROERP1.0" : config.VersaoAplicativo;

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = false
        };

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("pedRegEvento", Ns);
            writer.WriteAttributeString("versao", "1.00");

            writer.WriteStartElement("infPedReg", Ns);
            writer.WriteAttributeString("Id", idPedReg);

            WriteEl(writer, "tpAmb", tpAmb);
            WriteEl(writer, "verAplic", verAplic);
            WriteEl(writer, "dhEvento", dhEvento);
            WriteEl(writer, "CNPJAutor", emitCnpj.PadLeft(14, '0')[^14..]);
            WriteEl(writer, "chNFSe", chave);

            writer.WriteStartElement("e101101", Ns);
            WriteEl(writer, "xDesc", "Cancelamento de NFS-e");
            WriteEl(writer, "cMotivo", motivo);
            WriteEl(writer, "xMotivo", descricao);
            writer.WriteEndElement(); // e101101

            writer.WriteEndElement(); // infPedReg
            writer.WriteEndElement(); // pedRegEvento
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static void WriteEl(XmlWriter writer, string name, string value)
    {
        writer.WriteStartElement(name, Ns);
        writer.WriteString(value);
        writer.WriteEndElement();
    }
}
