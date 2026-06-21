using System.Globalization;
using System.Text;
using System.Xml;

namespace EUROERP.Infrastructure.Nfes;

/// <summary>Builds nacional NFS-e DPS XML (Simpliss / layout SPED).</summary>
internal static class NfesDpsBuilder
{
    private const string Ns = "http://www.sped.fazenda.gov.br/nfse";

    public static string Build(NfesEmissionWorkItem work, NfesConfigSnapshot config)
    {
        var emitCnpj = NfesTextHelper.CleanDigits(config.EmitCnpj);
        var emitIm = NfesTextHelper.CleanDigits(config.EmitIMun);
        var codMun = config.CodigoMunicipio;
        var cTribNac = NfesTextHelper.CleanDigits(config.CodigoTributacaoNacional);
        var cTribMunSource = config.CodigoTributacaoMunicipal;
        if (string.IsNullOrWhiteSpace(cTribMunSource) && !config.UseSimpliss)
            cTribMunSource = config.ServiceCode;
        var cTribMun = NfesTextHelper.CleanDigits(cTribMunSource);
        var cNbs = config.CodigoNbs;
        var serviceTax = config.ServiceTax;
        var versao = config.VersaoDps;
        var serie = config.SerieDps.PadLeft(5, '0')[^5..];
        var verAplic = config.VersaoAplicativo;
        var opSimpNac = config.OpSimpNac;
        var regEspTrib = config.RegEspTrib;
        var isTest = config.IsTestEnvironment;

        var personType = work.Client.PersonType?.Trim().ToUpperInvariant() ?? "J";
        var cpfCnpj = NfesTextHelper.CleanDigits(work.Client.CnpjPf ?? "");
        var tpInscPrestador = "2";
        var nDps = BuildNDps(work.RpsNumber);
        var idDps = $"DPS{codMun}{tpInscPrestador}{emitCnpj.PadLeft(14, '0')[^14..]}{serie}{nDps}";
        var discriminacao = BuildDiscriminacao(work);
        var emission = NfesTextHelper.GetBrazilEmissionInstant();
        var dhEmi = emission.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        var dCompet = emission.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var cMunTomador = NfesTextHelper.CleanDigits(work.Client.CMun ?? codMun);
        var cepTomador = NfesTextHelper.CleanDigits(work.Client.AddressZipCode ?? "0").PadLeft(8, '0')[^8..];
        var email = NfesTextHelper.Substring((work.Client.Email ?? "").Split('/')[0].Trim(), 80);
        var includeIbsCbs = config.IncludeIbsCbs;

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
            writer.WriteStartElement("DPS", Ns);
            writer.WriteAttributeString("versao", versao);

            writer.WriteStartElement("infDPS", Ns);
            writer.WriteAttributeString("Id", idDps);

            WriteEl(writer, "tpAmb", isTest ? "2" : "1");
            WriteEl(writer, "dhEmi", dhEmi);
            WriteEl(writer, "verAplic", verAplic);
            WriteEl(writer, "serie", serie);
            WriteEl(writer, "nDPS", nDps);
            WriteEl(writer, "dCompet", dCompet);
            WriteEl(writer, "tpEmit", "1");
            WriteEl(writer, "cLocEmi", codMun);

            writer.WriteStartElement("prest", Ns);
            WriteEl(writer, "CNPJ", emitCnpj.PadLeft(14, '0')[^14..]);
            if (!string.IsNullOrEmpty(emitIm))
                WriteEl(writer, "IM", emitIm);
            writer.WriteStartElement("regTrib", Ns);
            WriteEl(writer, "opSimpNac", opSimpNac);
            if (opSimpNac == "3")
                WriteEl(writer, "regApTribSN", config.RegApTribSn);
            WriteEl(writer, "regEspTrib", regEspTrib);
            writer.WriteEndElement(); // regTrib
            writer.WriteEndElement(); // prest

            writer.WriteStartElement("toma", Ns);
            if (personType == "F")
                WriteEl(writer, "CPF", cpfCnpj.PadLeft(11, '0')[^11..]);
            else
                WriteEl(writer, "CNPJ", cpfCnpj.PadLeft(14, '0')[^14..]);
            WriteEl(writer, "xNome", NfesTextHelper.CleanStringToXml(work.Client.SocialName ?? "", 150));

            writer.WriteStartElement("end", Ns);
            writer.WriteStartElement("endNac", Ns);
            WriteEl(writer, "cMun", cMunTomador);
            WriteEl(writer, "CEP", cepTomador);
            writer.WriteEndElement(); // endNac
            WriteEl(writer, "xLgr", NfesTextHelper.CleanStringToXml(work.Client.AddressStreet ?? "", 125));
            WriteEl(writer, "nro", NfesTextHelper.Substring(work.Client.AddressNumber ?? "S/N", 10));
            if (!string.IsNullOrWhiteSpace(work.Client.AddressComplement))
                WriteEl(writer, "xCpl", NfesTextHelper.CleanStringToXml(work.Client.AddressComplement, 60));
            WriteEl(writer, "xBairro", NfesTextHelper.CleanStringToXml(work.Client.AddressBlock ?? "", 60));
            writer.WriteEndElement(); // end

            var phone = NfesTextHelper.CleanDigits(work.Client.Phone ?? "");
            if (phone.Length >= 8)
                WriteEl(writer, "fone", phone);
            if (!string.IsNullOrWhiteSpace(email))
                WriteEl(writer, "email", email);
            writer.WriteEndElement(); // toma

            writer.WriteStartElement("serv", Ns);
            writer.WriteStartElement("locPrest", Ns);
            WriteEl(writer, "cLocPrestacao", codMun);
            writer.WriteEndElement(); // locPrest
            writer.WriteStartElement("cServ", Ns);
            WriteEl(writer, "cTribNac", cTribNac);
            if (!string.IsNullOrWhiteSpace(cTribMun))
                WriteEl(writer, "cTribMun", cTribMun);
            WriteEl(writer, "xDescServ", NfesTextHelper.CleanStringToXml(discriminacao, 2000));
            if (!string.IsNullOrWhiteSpace(cNbs))
                WriteEl(writer, "cNBS", NfesTextHelper.CleanDigits(cNbs));
            writer.WriteEndElement(); // cServ
            writer.WriteEndElement(); // serv

            writer.WriteStartElement("valores", Ns);
            writer.WriteStartElement("vServPrest", Ns);
            WriteEl(writer, "vServ", FormatDecimal(work.NetAmount));
            writer.WriteEndElement(); // vServPrest
            writer.WriteStartElement("trib", Ns);
            writer.WriteStartElement("tribMun", Ns);
            WriteEl(writer, "tribISSQN", "1");
            WriteEl(writer, "tpRetISSQN", "1");
            WriteEl(writer, "pAliq", FormatDecimal(serviceTax));
            writer.WriteEndElement(); // tribMun
            writer.WriteStartElement("totTrib", Ns);
            if (opSimpNac == "3" && !string.IsNullOrWhiteSpace(config.PercentualTotTribSn))
                WriteEl(writer, "pTotTribSN", config.PercentualTotTribSn);
            else
                WriteEl(writer, "indTotTrib", "0");
            writer.WriteEndElement(); // totTrib
            writer.WriteEndElement(); // trib
            writer.WriteEndElement(); // valores

            if (includeIbsCbs)
            {
                writer.WriteStartElement("IBSCBS", Ns);
                WriteEl(writer, "finNFSe", config.IbsCbsFinNfse);
                WriteEl(writer, "indFinal", config.IbsCbsIndFinal);
                WriteEl(writer, "cIndOp", config.IbsCbsIndOp);
                WriteEl(writer, "indDest", config.IbsCbsIndDest);
                writer.WriteStartElement("valores", Ns);
                writer.WriteStartElement("trib", Ns);
                writer.WriteStartElement("gIBSCBS", Ns);
                WriteEl(writer, "CST", config.IbsCbsCst);
                WriteEl(writer, "cClassTrib", config.IbsCbsClassTrib);
                writer.WriteEndElement(); // gIBSCBS
                writer.WriteEndElement(); // trib
                writer.WriteEndElement(); // valores
                writer.WriteEndElement(); // IBSCBS
            }

            writer.WriteEndElement(); // infDPS
            writer.WriteEndElement(); // DPS
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public static string BuildNDps(int rpsNumber)
    {
        if (rpsNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(rpsNumber));
        var text = rpsNumber.ToString(CultureInfo.InvariantCulture);
        if (text.Length > 14)
            return text[^15..];
        return "1" + text.PadLeft(14, '0');
    }

    private static string BuildDiscriminacao(NfesEmissionWorkItem work)
    {
        var sb = new StringBuilder();
        foreach (var line in work.ServiceLines)
        {
            sb.Append(line.Name).Append(" - Qtd: ").Append(line.Quantity.ToString(CultureInfo.InvariantCulture))
                .Append(" - Unit.R$ ").Append(line.UnitPrice.ToString("N2", CultureInfo.InvariantCulture))
                .Append(" - Total R$ ").Append(line.TotalPrice.ToString("N2", CultureInfo.InvariantCulture))
                .Append('\n');
        }
        sb.Append("OS ").Append(work.OrderId).Append('\n');
        if (!string.IsNullOrWhiteSpace(work.Order.CarPlate))
            sb.Append(work.Order.CarDescription).Append(" Placa: ").Append(work.Order.CarPlate).Append('\n');
        if (!string.IsNullOrWhiteSpace(work.Order.CarProblem))
            sb.Append("Dados adicionais: ").Append(work.Order.CarProblem).Append('\n');
        if (work.BtrDueDates.Count > 0)
        {
            var venc = "Vencs: " + string.Join("  ", work.BtrDueDates.Select(d => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
            foreach (var line in NfesTextHelper.CutLines(venc, 120))
                sb.Append(line).Append('\n');
        }
        return sb.ToString().Trim();
    }

    private static string FormatDecimal(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);

    private static void WriteEl(XmlWriter writer, string name, string value)
    {
        writer.WriteStartElement(name, Ns);
        writer.WriteString(value);
        writer.WriteEndElement();
    }
}
