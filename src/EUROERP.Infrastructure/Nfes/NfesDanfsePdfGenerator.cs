using System.Globalization;
using System.Xml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EUROERP.Infrastructure.Nfes;

internal static class NfesDanfsePdfGenerator
{
    public static byte[] Generate(string nfseXml)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var model = NfesDanfseModel.Parse(nfseXml);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, model));
                page.Content().Element(c => ComposeContent(c, model));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Consulta pública: ");
                    text.Span(model.ConsultaUrl).FontSize(7);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, NfesDanfseModel model)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text("DANFSe").FontSize(16).Bold();
            column.Item().AlignCenter().Text("Documento Auxiliar da Nota Fiscal de Serviço eletrônica").FontSize(10);
            column.Item().PaddingTop(8).Border(1).Padding(8).Column(box =>
            {
                box.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Número NFS-e: ").Bold();
                        t.Span(model.NumeroNfse ?? "—");
                    });
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Emissão: ").Bold();
                        t.Span(model.DataEmissao ?? "—");
                    });
                });
                box.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Chave de acesso: ").Bold();
                    t.Span(model.ChaveAcesso ?? "—").FontFamily(Fonts.Courier).FontSize(8);
                });
                if (!string.IsNullOrWhiteSpace(model.LocalEmissao))
                {
                    box.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("Município emissor: ").Bold();
                        t.Span(model.LocalEmissao);
                    });
                }
            });
        });
    }

    private static void ComposeContent(IContainer container, NfesDanfseModel model)
    {
        container.PaddingTop(12).Column(column =>
        {
            column.Item().Element(c => ComposePartyBlock(c, "Prestador do serviço", model.Prestador));
            column.Item().PaddingTop(8).Element(c => ComposePartyBlock(c, "Tomador do serviço", model.Tomador));
            column.Item().PaddingTop(8).Element(c => ComposeServiceBlock(c, model));
            column.Item().PaddingTop(8).Element(c => ComposeValuesBlock(c, model));
        });
    }

    private static void ComposePartyBlock(IContainer container, string title, NfesDanfseParty party)
    {
        container.Border(1).Padding(8).Column(column =>
        {
            column.Item().Text(title).Bold().FontSize(10);
            column.Item().PaddingTop(4).Text(party.Nome ?? "—");
            if (!string.IsNullOrWhiteSpace(party.Documento))
                column.Item().Text($"CNPJ/CPF: {party.Documento}");
            if (!string.IsNullOrWhiteSpace(party.InscricaoMunicipal))
                column.Item().Text($"Inscrição Municipal: {party.InscricaoMunicipal}");
            if (!string.IsNullOrWhiteSpace(party.Endereco))
                column.Item().Text(party.Endereco);
            if (!string.IsNullOrWhiteSpace(party.Email))
                column.Item().Text($"E-mail: {party.Email}");
            if (!string.IsNullOrWhiteSpace(party.Telefone))
                column.Item().Text($"Telefone: {party.Telefone}");
        });
    }

    private static void ComposeServiceBlock(IContainer container, NfesDanfseModel model)
    {
        container.Border(1).Padding(8).Column(column =>
        {
            column.Item().Text("Discriminação do serviço").Bold().FontSize(10);
            if (!string.IsNullOrWhiteSpace(model.CodigoTributacaoNacional))
            {
                column.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Código tributação nacional: ").Bold();
                    t.Span(model.CodigoTributacaoNacional);
                });
            }
            if (!string.IsNullOrWhiteSpace(model.DescricaoTributacaoNacional))
                column.Item().Text(model.DescricaoTributacaoNacional).FontSize(8).Italic();
            column.Item().PaddingTop(4).Text(model.DescricaoServico ?? "—");
            if (!string.IsNullOrWhiteSpace(model.LocalPrestacao))
            {
                column.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Local da prestação: ").Bold();
                    t.Span(model.LocalPrestacao);
                });
            }
            if (!string.IsNullOrWhiteSpace(model.Competencia))
            {
                column.Item().Text(t =>
                {
                    t.Span("Competência: ").Bold();
                    t.Span(model.Competencia);
                });
            }
        });
    }

    private static void ComposeValuesBlock(IContainer container, NfesDanfseModel model)
    {
        container.Border(1).Padding(8).Column(column =>
        {
            column.Item().Text("Valores").Bold().FontSize(10);
            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                });

                AddValueRow(table, "Valor dos serviços", model.ValorServicos);
                AddValueRow(table, "Base de cálculo ISS", model.BaseCalculo);
                AddValueRow(table, "Alíquota ISS (%)", model.AliquotaIss);
                AddValueRow(table, "Valor ISS", model.ValorIss);
                AddValueRow(table, "Valor líquido", model.ValorLiquido, bold: true);
            });
        });
    }

    private static void AddValueRow(TableDescriptor table, string label, string? value, bool bold = false)
    {
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Text(label);
        var cell = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).AlignRight();
        if (bold)
            cell.Text(value ?? "—").Bold();
        else
            cell.Text(value ?? "—");
    }
}

internal sealed class NfesDanfseModel
{
    public string? ChaveAcesso { get; init; }
    public string? NumeroNfse { get; init; }
    public string? DataEmissao { get; init; }
    public string? LocalEmissao { get; init; }
    public string? LocalPrestacao { get; init; }
    public string? Competencia { get; init; }
    public string? CodigoTributacaoNacional { get; init; }
    public string? DescricaoTributacaoNacional { get; init; }
    public string? DescricaoServico { get; init; }
    public string? ValorServicos { get; init; }
    public string? BaseCalculo { get; init; }
    public string? AliquotaIss { get; init; }
    public string? ValorIss { get; init; }
    public string? ValorLiquido { get; init; }
    public string ConsultaUrl { get; init; } = "";
    public NfesDanfseParty Prestador { get; init; } = new();
    public NfesDanfseParty Tomador { get; init; } = new();

    public static NfesDanfseModel Parse(string nfseXml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(nfseXml);

        var chave = doc.GetElementsByTagName("infNFSe").Cast<XmlElement>().FirstOrDefault()?.GetAttribute("Id")?.Trim();
        var numero = Text(doc, "nNFSe") ?? Text(doc, "nDFSe");
        var dhEmi = Text(doc, "dhEmi") ?? Text(doc, "dhProc");
        var localEmi = Text(doc, "xLocEmi");
        var localPrest = Text(doc, "xLocPrestacao");
        var competencia = Text(doc, "dCompet");
        var cTribNac = Text(doc, "cTribNac");
        var xTribNac = Text(doc, "xTribNac");
        var xDescServ = Text(doc, "xDescServ");

        var vServ = Money(Text(doc, "vServ") ?? Text(doc, "vBC"));
        var vBc = Money(Text(doc, "vBC"));
        var pAliq = Percent(Text(doc, "pAliq") ?? Text(doc, "pAliqAplic"));
        var vIss = Money(Text(doc, "vISSQN"));
        var vLiq = Money(Text(doc, "vLiq"));

        var prestador = ParseParty(
            Text(doc, "CNPJ", parentTag: "prest") ?? Text(doc, "CNPJ", parentTag: "emit"),
            Text(doc, "IM", parentTag: "prest") ?? Text(doc, "IM", parentTag: "emit"),
            Text(doc, "xNome", parentTag: "emit"),
            BuildAddress(doc, "emit"),
            Text(doc, "email", parentTag: "emit"),
            null);

        var tomador = ParseParty(
            Text(doc, "CNPJ", parentTag: "toma") ?? Text(doc, "CPF", parentTag: "toma"),
            null,
            Text(doc, "xNome", parentTag: "toma"),
            BuildAddress(doc, "toma"),
            Text(doc, "email", parentTag: "toma"),
            Text(doc, "fone", parentTag: "toma"));

        var consultaUrl = !string.IsNullOrWhiteSpace(chave)
            ? "https://www.nfse.gov.br/ConsultaPublica/?tpc=1&chave=" + Uri.EscapeDataString(chave)
            : "https://www.nfse.gov.br/ConsultaPublica/";

        return new NfesDanfseModel
        {
            ChaveAcesso = chave,
            NumeroNfse = numero,
            DataEmissao = FormatDateTime(dhEmi),
            LocalEmissao = localEmi,
            LocalPrestacao = localPrest,
            Competencia = FormatDate(competencia),
            CodigoTributacaoNacional = cTribNac,
            DescricaoTributacaoNacional = xTribNac,
            DescricaoServico = xDescServ,
            ValorServicos = vServ,
            BaseCalculo = vBc,
            AliquotaIss = pAliq,
            ValorIss = vIss,
            ValorLiquido = vLiq,
            ConsultaUrl = consultaUrl,
            Prestador = prestador,
            Tomador = tomador
        };
    }

    private static NfesDanfseParty ParseParty(string? doc, string? im, string? nome, string? endereco, string? email, string? fone) =>
        new()
        {
            Documento = FormatDocument(doc),
            InscricaoMunicipal = im,
            Nome = nome,
            Endereco = endereco,
            Email = email,
            Telefone = fone
        };

    private static string? Text(XmlDocument doc, string localName, string? parentTag = null)
    {
        foreach (XmlNode node in doc.GetElementsByTagName(localName))
        {
            if (parentTag != null)
            {
                var parent = node.ParentNode;
                if (parent?.LocalName != parentTag && parent?.Name != parentTag)
                    continue;
            }

            var value = node.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? BuildAddress(XmlDocument doc, string parentTag)
    {
        foreach (XmlNode parent in doc.GetElementsByTagName(parentTag))
        {
            var log = FindChildText(parent, "xLgr");
            var nro = FindChildText(parent, "nro");
            var cpl = FindChildText(parent, "xCpl");
            var bairro = FindChildText(parent, "xBairro");
            var uf = FindChildText(parent, "UF");
            var cep = FindChildText(parent, "CEP");

            if (string.IsNullOrWhiteSpace(log) && string.IsNullOrWhiteSpace(nro))
                continue;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(log))
                parts.Add(log);
            if (!string.IsNullOrWhiteSpace(nro))
                parts.Add(nro);
            if (!string.IsNullOrWhiteSpace(cpl))
                parts.Add(cpl);
            if (!string.IsNullOrWhiteSpace(bairro))
                parts.Add(bairro);
            if (!string.IsNullOrWhiteSpace(uf))
                parts.Add(uf);
            if (!string.IsNullOrWhiteSpace(cep))
                parts.Add("CEP " + FormatCep(cep));

            return string.Join(", ", parts);
        }

        return null;
    }

    private static string? FindChildText(XmlNode parent, string localName)
    {
        var nodes = parent.SelectNodes($".//*[local-name()='{localName}']");
        if (nodes == null)
            return null;

        foreach (XmlNode node in nodes)
        {
            var value = node.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? FormatDocument(string? digits)
    {
        if (string.IsNullOrWhiteSpace(digits))
            return null;

        digits = new string(digits.Where(char.IsDigit).ToArray());
        if (digits.Length == 14)
            return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
        if (digits.Length == 11)
            return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
        return digits;
    }

    private static string? FormatCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return null;
        cep = new string(cep.Where(char.IsDigit).ToArray());
        return cep.Length == 8 ? $"{cep[..5]}-{cep[5..]}" : cep;
    }

    private static string? Money(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return value;
        return amount.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
    }

    private static string? Percent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return value;
        return amount.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
    }

    private static string? FormatDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return dto.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"));
        return value;
    }

    private static string? FormatDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
        return value;
    }
}

internal sealed class NfesDanfseParty
{
    public string? Documento { get; init; }
    public string? InscricaoMunicipal { get; init; }
    public string? Nome { get; init; }
    public string? Endereco { get; init; }
    public string? Email { get; init; }
    public string? Telefone { get; init; }
}
