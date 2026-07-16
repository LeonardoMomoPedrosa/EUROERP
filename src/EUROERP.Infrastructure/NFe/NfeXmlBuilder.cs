using System.Globalization;
using System.Text;
using System.Xml;

namespace EUROERP.Infrastructure.NFe;

/// <summary>
/// Monta o XML da NFe 4.0 (TNFe) a partir dos dados do pedido e emitente.
/// </summary>
public interface INfeXmlBuilder
{
    /// <summary>
    /// Gera o documento XML da NFe (elemento NFe com infNFe). O infNFe deve ter Id="NFe" + chave para assinatura.
    /// </summary>
    XmlDocument BuildNfeXml(NfeBuildInput input);
}

public sealed class NfeBuildInput
{
    public string Chave { get; set; } = "";
    public int NfeNumber { get; set; }
    public int OrderId { get; set; }
    public string Cnf8Digits { get; set; } = "";
    public string DhEmi { get; set; } = ""; // AAAA-MM-DDThh:mm:ss-03:00
    public string NatOp { get; set; } = "VENDA";
    public string TpAmb { get; set; } = "1"; // 1 Produção, 2 Homologação
    public string IdDest { get; set; } = "1"; // 1 Interna, 2 Interestadual
    public string VerProc { get; set; } = "1.0";

    public NfeEmitInput Emit { get; set; } = new();
    public NfeDestInput Dest { get; set; } = new();
    public List<NfeDetInput> Det { get; set; } = new();
    public decimal TotalVnf { get; set; }
    /// <summary>Valor do frete (incluído no primeiro item e no total ICMSTot.vFrete).</summary>
    public decimal TotalVfrete { get; set; }
    /// <summary>Desconto total (ICMSTot.vDesc). Deve bater com a soma dos vDesc dos itens.</summary>
    public decimal TotalVdesc { get; set; }
    /// <summary>Outras despesas (ICMSTot.vOutro).</summary>
    public decimal TotalVoutro { get; set; }
    public byte ModFrete { get; set; } // 0 Emitente, 1 Destinatário, 9 Sem frete
    public string? VolEsp { get; set; }
    public string? VolQty { get; set; }
    public string? PesoB { get; set; }
    public string? PesoL { get; set; }
    /// <summary>Informações complementares de interesse do contribuinte (infCpl). Opcional; máx. 5000 caracteres.</summary>
    public string? InfCpl { get; set; }
    /// <summary>ICMS alíquota % (regime normal, CRT 3).</summary>
    public decimal IcmsAliqPercent { get; set; } = 18;
    public decimal PisAliqPercent { get; set; } = 1.65m;
    public decimal CofinsAliqPercent { get; set; } = 3m;
}

public sealed class NfeEmitInput
{
    public string Cnpj { get; set; } = "";
    public string RazaoSocial { get; set; } = "";
    public string Fantasia { get; set; } = "";
    public string Logradouro { get; set; } = "";
    public string Numero { get; set; } = "";
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = "";
    public string CodigoMun { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Cep { get; set; } = "";
    public string? Fone { get; set; }
    public string? Ie { get; set; }
    public string Crt { get; set; } = "1"; // 1 Simples Nacional, 2 Simples excesso, 3 Regime Normal
}

public sealed class NfeDestInput
{
    public bool IsCpf { get; set; }
    public string CpfCnpj { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Logradouro { get; set; } = "";
    public string Numero { get; set; } = "";
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = "";
    public string CodigoMun { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Cep { get; set; } = "";
    public string? Fone { get; set; }
    public string? Email { get; set; }
    public string IndIeDest { get; set; } = "1"; // 1 Contribuinte, 2 Isento, 9 Não contribuinte
    public string? Ie { get; set; }
}

public sealed class NfeDetInput
{
    public int NItem { get; set; }
    public string CProd { get; set; } = "";
    public string XProd { get; set; } = "";
    public string Ncm { get; set; } = "00000000";
    public string Cfop { get; set; } = "5102";
    public string UCom { get; set; } = "UN";
    public decimal QCom { get; set; }
    public decimal VUnCom { get; set; }
    public decimal VProd { get; set; }
    public decimal VDesc { get; set; }
    public decimal VFrete { get; set; }
    public decimal VOutro { get; set; }
    public string Csosn { get; set; } = "102"; // 102 Isento, 500 ST já cobrada, etc.
    public string Origem { get; set; } = "0"; // 0 Nacional
}

public class NfeXmlBuilder : INfeXmlBuilder
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public XmlDocument BuildNfeXml(NfeBuildInput input)
    {
        const string ns = "http://www.portalfiscal.inf.br/nfe";
        var doc = new XmlDocument();
        doc.PreserveWhitespace = false;

        var decl = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        doc.AppendChild(decl);

        var nfe = doc.CreateElement("NFe", ns);
        doc.AppendChild(nfe);

        var infNFe = doc.CreateElement("infNFe", ns);
        infNFe.SetAttribute("Id", "NFe" + input.Chave);
        infNFe.SetAttribute("versao", "4.00");
        nfe.AppendChild(infNFe);

        AppendIde(doc, infNFe, input, ns);
        AppendEmit(doc, infNFe, input.Emit, ns);
        AppendDest(doc, infNFe, input.Dest, ns);
        foreach (var det in input.Det)
            AppendDet(doc, infNFe, det, input, ns);
        AppendTotal(doc, infNFe, input, ns);
        AppendTransp(doc, infNFe, input, ns);
        AppendPag(doc, infNFe, input.TotalVnf, ns);
        if (!string.IsNullOrWhiteSpace(input.InfCpl))
            AppendInfAdic(doc, infNFe, input.InfCpl, ns);

        return doc;
    }

    private static void AppendIde(XmlDocument doc, XmlElement parent, NfeBuildInput input, string ns)
    {
        var ide = doc.CreateElement("ide", ns);
        AppendElem(doc, ide, ns, "cUF", "35");
        AppendElem(doc, ide, ns, "cNF", input.Cnf8Digits);
        AppendElem(doc, ide, ns, "natOp", CleanToXml(input.NatOp, 60));
        AppendElem(doc, ide, ns, "mod", "55");
        AppendElem(doc, ide, ns, "serie", "0");
        AppendElem(doc, ide, ns, "nNF", input.NfeNumber.ToString(Inv));
        AppendElem(doc, ide, ns, "dhEmi", input.DhEmi);
        AppendElem(doc, ide, ns, "dhSaiEnt", input.DhEmi);
        AppendElem(doc, ide, ns, "tpNF", "1");
        AppendElem(doc, ide, ns, "idDest", input.IdDest);
        AppendElem(doc, ide, ns, "cMunFG", input.Emit.CodigoMun);
        AppendElem(doc, ide, ns, "tpImp", "1");
        AppendElem(doc, ide, ns, "tpEmis", "1");
        AppendElem(doc, ide, ns, "cDV", input.Chave.Length >= 44 ? input.Chave.Substring(43, 1) : "0");
        AppendElem(doc, ide, ns, "tpAmb", input.TpAmb);
        AppendElem(doc, ide, ns, "finNFe", "1");
        AppendElem(doc, ide, ns, "indFinal", "1");
        AppendElem(doc, ide, ns, "indPres", "1"); // 1-Operação presencial
        AppendElem(doc, ide, ns, "procEmi", "0");
        AppendElem(doc, ide, ns, "verProc", CleanToXml(input.VerProc, 20));
        parent.AppendChild(ide);
    }

    private static void AppendEmit(XmlDocument doc, XmlElement parent, NfeEmitInput e, string ns)
    {
        var emit = doc.CreateElement("emit", ns);
        AppendElem(doc, emit, ns, "CNPJ", NfeChaveHelper.CleanDigits(e.Cnpj));
        AppendElem(doc, emit, ns, "xNome", CleanToXml(e.RazaoSocial, 60));
        AppendElem(doc, emit, ns, "xFant", CleanToXml(e.Fantasia ?? e.RazaoSocial, 60));

        var enderEmit = doc.CreateElement("enderEmit", ns);
        AppendElem(doc, enderEmit, ns, "xLgr", CleanToXml(e.Logradouro, 60));
        AppendElem(doc, enderEmit, ns, "nro", CleanToXml(e.Numero, 60));
        if (!string.IsNullOrWhiteSpace(e.Complemento)) AppendElem(doc, enderEmit, ns, "xCpl", CleanToXml(e.Complemento, 60));
        AppendElem(doc, enderEmit, ns, "xBairro", CleanToXml(e.Bairro, 60));
        AppendElem(doc, enderEmit, ns, "cMun", e.CodigoMun);
        AppendElem(doc, enderEmit, ns, "xMun", CleanToXml(e.Municipio, 60));
        AppendElem(doc, enderEmit, ns, "UF", e.Uf.Length >= 2 ? e.Uf.Substring(0, 2) : e.Uf);
        AppendElem(doc, enderEmit, ns, "CEP", NfeChaveHelper.CleanDigits(e.Cep).PadRight(8, '0').Substring(0, 8));
        AppendElem(doc, enderEmit, ns, "cPais", "1058");
        AppendElem(doc, enderEmit, ns, "xPais", "BRASIL");
        if (!string.IsNullOrWhiteSpace(e.Fone)) AppendElem(doc, enderEmit, ns, "fone", NfeChaveHelper.CleanDigits(e.Fone).Length <= 14 ? NfeChaveHelper.CleanDigits(e.Fone) : "");
        emit.AppendChild(enderEmit);

        if (!string.IsNullOrWhiteSpace(e.Ie)) AppendElem(doc, emit, ns, "IE", e.Ie);
        AppendElem(doc, emit, ns, "CRT", e.Crt);
        parent.AppendChild(emit);
    }

    private static void AppendDest(XmlDocument doc, XmlElement parent, NfeDestInput d, string ns)
    {
        var dest = doc.CreateElement("dest", ns);
        var cpfCnpj = NfeChaveHelper.CleanDigits(d.CpfCnpj);
        if (d.IsCpf)
            AppendElem(doc, dest, ns, "CPF", cpfCnpj.Length <= 11 ? cpfCnpj.PadLeft(11, '0') : cpfCnpj);
        else
            AppendElem(doc, dest, ns, "CNPJ", cpfCnpj.Length <= 14 ? cpfCnpj.PadLeft(14, '0') : cpfCnpj);
        AppendElem(doc, dest, ns, "xNome", CleanToXml(d.Nome, 60));

        var enderDest = doc.CreateElement("enderDest", ns);
        AppendElem(doc, enderDest, ns, "xLgr", CleanToXml(d.Logradouro, 60));
        AppendElem(doc, enderDest, ns, "nro", CleanToXml(d.Numero, 60));
        if (!string.IsNullOrWhiteSpace(d.Complemento)) AppendElem(doc, enderDest, ns, "xCpl", CleanToXml(d.Complemento, 60));
        AppendElem(doc, enderDest, ns, "xBairro", CleanToXml(d.Bairro, 60));
        AppendElem(doc, enderDest, ns, "cMun", d.CodigoMun);
        AppendElem(doc, enderDest, ns, "xMun", CleanToXml(d.Municipio, 60));
        AppendElem(doc, enderDest, ns, "UF", d.Uf.Length >= 2 ? d.Uf.Substring(0, 2) : d.Uf);
        AppendElem(doc, enderDest, ns, "CEP", NfeChaveHelper.CleanDigits(d.Cep).PadRight(8, '0').Substring(0, 8));
        AppendElem(doc, enderDest, ns, "cPais", "1058");
        AppendElem(doc, enderDest, ns, "xPais", "BRASIL");
        if (!string.IsNullOrWhiteSpace(d.Fone)) AppendElem(doc, enderDest, ns, "fone", NfeChaveHelper.CleanDigits(d.Fone).Length <= 14 ? NfeChaveHelper.CleanDigits(d.Fone) : "");
        dest.AppendChild(enderDest);

        AppendElem(doc, dest, ns, "indIEDest", d.IndIeDest);
        if (!string.IsNullOrWhiteSpace(d.Ie)) AppendElem(doc, dest, ns, "IE", d.Ie);
        if (!string.IsNullOrWhiteSpace(d.Email)) AppendElem(doc, dest, ns, "email", CleanToXml(d.Email, 60));
        parent.AppendChild(dest);
    }

    private static void AppendDet(XmlDocument doc, XmlElement parent, NfeDetInput det, NfeBuildInput input, string ns)
    {
        var detEl = doc.CreateElement("det", ns);
        detEl.SetAttribute("nItem", det.NItem.ToString(Inv));

        var prod = doc.CreateElement("prod", ns);
        AppendElem(doc, prod, ns, "cProd", CleanToXml(det.CProd, 60));
        AppendElem(doc, prod, ns, "cEAN", "SEM GTIN");
        AppendElem(doc, prod, ns, "xProd", CleanToXml(det.XProd, 120));
        AppendElem(doc, prod, ns, "NCM", NfeChaveHelper.CleanDigits(det.Ncm).PadRight(8, '0').Substring(0, 8));
        AppendElem(doc, prod, ns, "CFOP", det.Cfop.Replace(".", ""));
        AppendElem(doc, prod, ns, "uCom", det.UCom);
        AppendElem(doc, prod, ns, "qCom", FmtDec(det.QCom));
        AppendElem(doc, prod, ns, "vUnCom", FmtDec(det.VUnCom));
        AppendElem(doc, prod, ns, "vProd", FmtDec(det.VProd));
        AppendElem(doc, prod, ns, "cEANTrib", "SEM GTIN");
        AppendElem(doc, prod, ns, "uTrib", det.UCom);
        AppendElem(doc, prod, ns, "qTrib", FmtDec(det.QCom));
        AppendElem(doc, prod, ns, "vUnTrib", FmtDec(det.VUnCom));
        if (det.VFrete > 0) AppendElem(doc, prod, ns, "vFrete", FmtDec(det.VFrete));
        if (det.VDesc > 0) AppendElem(doc, prod, ns, "vDesc", FmtDec(det.VDesc));
        if (det.VOutro > 0) AppendElem(doc, prod, ns, "vOutro", FmtDec(det.VOutro));
        AppendElem(doc, prod, ns, "indTot", "1");
        detEl.AppendChild(prod);

        var imposto = doc.CreateElement("imposto", ns);
        var lineBase = det.VProd - det.VDesc + det.VFrete + det.VOutro;
        if (lineBase < 0) lineBase = 0;
        var crt = input.Emit.Crt ?? "1";

        var icms = doc.CreateElement("ICMS", ns);
        if (crt == "3")
        {
            var vIcms = Math.Round(lineBase * input.IcmsAliqPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var icms00 = doc.CreateElement("ICMS00", ns);
            AppendElem(doc, icms00, ns, "orig", det.Origem);
            AppendElem(doc, icms00, ns, "CST", "00");
            AppendElem(doc, icms00, ns, "modBC", "0");
            AppendElem(doc, icms00, ns, "vBC", FmtDec(lineBase));
            AppendElem(doc, icms00, ns, "pICMS", FmtDec(input.IcmsAliqPercent));
            AppendElem(doc, icms00, ns, "vICMS", FmtDec(vIcms));
            icms.AppendChild(icms00);
        }
        else
        {
            var icmsSn = doc.CreateElement("ICMSSN102", ns);
            AppendElem(doc, icmsSn, ns, "orig", det.Origem);
            AppendElem(doc, icmsSn, ns, "CSOSN", det.Csosn);
            icms.AppendChild(icmsSn);
        }
        imposto.AppendChild(icms);

        if (crt == "3")
        {
            var vPis = Math.Round(lineBase * input.PisAliqPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var vCofins = Math.Round(lineBase * input.CofinsAliqPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var pis = doc.CreateElement("PIS", ns);
            var pisAliq = doc.CreateElement("PISAliq", ns);
            AppendElem(doc, pisAliq, ns, "CST", "01");
            AppendElem(doc, pisAliq, ns, "vBC", FmtDec(lineBase));
            AppendElem(doc, pisAliq, ns, "pPIS", FmtDec(input.PisAliqPercent));
            AppendElem(doc, pisAliq, ns, "vPIS", FmtDec(vPis));
            pis.AppendChild(pisAliq);
            imposto.AppendChild(pis);

            var cofins = doc.CreateElement("COFINS", ns);
            var cofinsAliq = doc.CreateElement("COFINSAliq", ns);
            AppendElem(doc, cofinsAliq, ns, "CST", "01");
            AppendElem(doc, cofinsAliq, ns, "vBC", FmtDec(lineBase));
            AppendElem(doc, cofinsAliq, ns, "pCOFINS", FmtDec(input.CofinsAliqPercent));
            AppendElem(doc, cofinsAliq, ns, "vCOFINS", FmtDec(vCofins));
            cofins.AppendChild(cofinsAliq);
            imposto.AppendChild(cofins);
        }
        else
        {
            var pis = doc.CreateElement("PIS", ns);
            var pisNt = doc.CreateElement("PISNT", ns);
            AppendElem(doc, pisNt, ns, "CST", "08");
            pis.AppendChild(pisNt);
            imposto.AppendChild(pis);

            var cofins = doc.CreateElement("COFINS", ns);
            var cofinsNt = doc.CreateElement("COFINSNT", ns);
            AppendElem(doc, cofinsNt, ns, "CST", "08");
            cofins.AppendChild(cofinsNt);
            imposto.AppendChild(cofins);
        }

        // IBSCBS - Implementação básica para atender obrigatoriedade a partir de 2026
        var ibscbs = doc.CreateElement("IBSCBS", ns);
        AppendElem(doc, ibscbs, ns, "CST", "000");
        AppendElem(doc, ibscbs, ns, "cClassTrib", "000001");
        var gIBSCBS = doc.CreateElement("gIBSCBS", ns);
        AppendElem(doc, gIBSCBS, ns, "vBC", "0.00");
        var gIBSUF = doc.CreateElement("gIBSUF", ns);
        AppendElem(doc, gIBSUF, ns, "pIBSUF", "0.1000");
        AppendElem(doc, gIBSUF, ns, "vIBSUF", "0.00");
        gIBSCBS.AppendChild(gIBSUF);
        var gIBSMun = doc.CreateElement("gIBSMun", ns);
        AppendElem(doc, gIBSMun, ns, "pIBSMun", "0.0000");
        AppendElem(doc, gIBSMun, ns, "vIBSMun", "0.00");
        gIBSCBS.AppendChild(gIBSMun);
        AppendElem(doc, gIBSCBS, ns, "vIBS", "0.00");
        var gCBS = doc.CreateElement("gCBS", ns);
        AppendElem(doc, gCBS, ns, "pCBS", "0.9000");
        AppendElem(doc, gCBS, ns, "vCBS", "0.00");
        gIBSCBS.AppendChild(gCBS);
        ibscbs.AppendChild(gIBSCBS);
        imposto.AppendChild(ibscbs);

        detEl.AppendChild(imposto);
        parent.AppendChild(detEl);
    }

    private static void AppendTotal(XmlDocument doc, XmlElement parent, NfeBuildInput input, string ns)
    {
        var vProd = input.Det.Sum(d => d.VProd);
        var vFrete = input.TotalVfrete;
        var vNf = input.TotalVnf;
        var crt = input.Emit.Crt ?? "1";
        decimal vBc = 0, vIcms = 0, vPis = 0, vCofins = 0;
        if (crt == "3")
        {
            foreach (var d in input.Det)
            {
                var lineBase = d.VProd - d.VDesc + d.VFrete + d.VOutro;
                if (lineBase < 0) lineBase = 0;
                vBc += lineBase;
                vIcms += Math.Round(lineBase * input.IcmsAliqPercent / 100m, 2, MidpointRounding.AwayFromZero);
                vPis += Math.Round(lineBase * input.PisAliqPercent / 100m, 2, MidpointRounding.AwayFromZero);
                vCofins += Math.Round(lineBase * input.CofinsAliqPercent / 100m, 2, MidpointRounding.AwayFromZero);
            }
        }

        var total = doc.CreateElement("total", ns);
        var icmsTot = doc.CreateElement("ICMSTot", ns);
        AppendElem(doc, icmsTot, ns, "vBC", FmtDec(crt == "3" ? vBc : 0));
        AppendElem(doc, icmsTot, ns, "vICMS", FmtDec(crt == "3" ? vIcms : 0));
        AppendElem(doc, icmsTot, ns, "vICMSDeson", "0.00");
        AppendElem(doc, icmsTot, ns, "vFCP", "0.00");
        AppendElem(doc, icmsTot, ns, "vBCST", "0.00");
        AppendElem(doc, icmsTot, ns, "vST", "0.00");
        AppendElem(doc, icmsTot, ns, "vFCPST", "0.00");
        AppendElem(doc, icmsTot, ns, "vFCPSTRet", "0.00");
        AppendElem(doc, icmsTot, ns, "vProd", FmtDec(vProd));
        AppendElem(doc, icmsTot, ns, "vFrete", FmtDec(vFrete));
        AppendElem(doc, icmsTot, ns, "vSeg", "0.00");
        AppendElem(doc, icmsTot, ns, "vDesc", FmtDec(input.TotalVdesc));
        AppendElem(doc, icmsTot, ns, "vII", "0.00");
        AppendElem(doc, icmsTot, ns, "vIPI", "0.00");
        AppendElem(doc, icmsTot, ns, "vIPIDevol", "0.00");
        AppendElem(doc, icmsTot, ns, "vPIS", FmtDec(crt == "3" ? vPis : 0));
        AppendElem(doc, icmsTot, ns, "vCOFINS", FmtDec(crt == "3" ? vCofins : 0));
        AppendElem(doc, icmsTot, ns, "vOutro", FmtDec(input.TotalVoutro));
        AppendElem(doc, icmsTot, ns, "vNF", FmtDec(vNf));
        total.AppendChild(icmsTot);

        // IBSCBSTot - Implementação básica para atender obrigatoriedade a partir de 2026
        var ibscbsTot = doc.CreateElement("IBSCBSTot", ns);
        AppendElem(doc, ibscbsTot, ns, "vBCIBSCBS", "0.00");
        var gIBSTot = doc.CreateElement("gIBS", ns);
        var gIBSUFTot = doc.CreateElement("gIBSUF", ns);
        AppendElem(doc, gIBSUFTot, ns, "vDif", "0.00");
        AppendElem(doc, gIBSUFTot, ns, "vDevTrib", "0.00");
        AppendElem(doc, gIBSUFTot, ns, "vIBSUF", "0.00");
        gIBSTot.AppendChild(gIBSUFTot);
        var gIBSMunTot = doc.CreateElement("gIBSMun", ns);
        AppendElem(doc, gIBSMunTot, ns, "vDif", "0.00");
        AppendElem(doc, gIBSMunTot, ns, "vDevTrib", "0.00");
        AppendElem(doc, gIBSMunTot, ns, "vIBSMun", "0.00");
        gIBSTot.AppendChild(gIBSMunTot);
        AppendElem(doc, gIBSTot, ns, "vIBS", "0.00");
        AppendElem(doc, gIBSTot, ns, "vCredPres", "0.00");
        AppendElem(doc, gIBSTot, ns, "vCredPresCondSus", "0.00");
        ibscbsTot.AppendChild(gIBSTot);
        var gCBSTot = doc.CreateElement("gCBS", ns);
        AppendElem(doc, gCBSTot, ns, "vDif", "0.00");
        AppendElem(doc, gCBSTot, ns, "vDevTrib", "0.00");
        AppendElem(doc, gCBSTot, ns, "vCBS", "0.00");
        AppendElem(doc, gCBSTot, ns, "vCredPres", "0.00");
        AppendElem(doc, gCBSTot, ns, "vCredPresCondSus", "0.00");
        ibscbsTot.AppendChild(gCBSTot);
        total.AppendChild(ibscbsTot);

        parent.AppendChild(total);
    }

    private static void AppendTransp(XmlDocument doc, XmlElement parent, NfeBuildInput input, string ns)
    {
        var transp = doc.CreateElement("transp", ns);
        AppendElem(doc, transp, ns, "modFrete", input.ModFrete.ToString(Inv));
        if (!string.IsNullOrWhiteSpace(input.VolEsp) || !string.IsNullOrWhiteSpace(input.VolQty) || !string.IsNullOrWhiteSpace(input.PesoB))
        {
            var vol = doc.CreateElement("vol", ns);
            if (!string.IsNullOrWhiteSpace(input.VolQty)) AppendElem(doc, vol, ns, "qVol", input.VolQty);
            if (!string.IsNullOrWhiteSpace(input.VolEsp)) AppendElem(doc, vol, ns, "esp", CleanToXml(input.VolEsp, 60));
            if (!string.IsNullOrWhiteSpace(input.PesoL)) AppendElem(doc, vol, ns, "pesoL", input.PesoL);
            if (!string.IsNullOrWhiteSpace(input.PesoB)) AppendElem(doc, vol, ns, "pesoB", input.PesoB);
            transp.AppendChild(vol);
        }
        parent.AppendChild(transp);
    }

    private static void AppendPag(XmlDocument doc, XmlElement parent, decimal vNf, string ns)
    {
        var pag = doc.CreateElement("pag", ns);
        var detPag = doc.CreateElement("detPag", ns);
        AppendElem(doc, detPag, ns, "indPag", "0"); // 0 À vista
        AppendElem(doc, detPag, ns, "tPag", "01"); // 01 Dinheiro
        AppendElem(doc, detPag, ns, "vPag", FmtDec(vNf));
        pag.AppendChild(detPag);
        parent.AppendChild(pag);
    }

    private static void AppendInfAdic(XmlDocument doc, XmlElement parent, string infCpl, string ns)
    {
        var text = infCpl.Trim();
        if (string.IsNullOrEmpty(text)) return;
        if (text.Length > 5000) text = text.Substring(0, 5000); // limite do schema
        var infAdic = doc.CreateElement("infAdic", ns);
        var infCplEl = doc.CreateElement("infCpl", ns);
        infCplEl.AppendChild(doc.CreateTextNode(text)); // CreateTextNode escapa automaticamente
        infAdic.AppendChild(infCplEl);
        parent.AppendChild(infAdic);
    }

    private static void AppendElem(XmlDocument doc, XmlElement parent, string ns, string localName, string value)
    {
        var el = doc.CreateElement(localName, ns);
        el.AppendChild(doc.CreateTextNode(value ?? ""));
        parent.AppendChild(el);
    }

    private static string FmtDec(decimal d) => d.ToString("0.00", Inv);
    private static string CleanToXml(string? s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("&", "").Replace("<", "").Replace(">", "").Replace("\"", "").Replace("'", "").Trim();
        return s.Length > maxLen ? s.Substring(0, maxLen) : s;
    }
}
