using System.Globalization;
using System.Text;
using System.Xml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Configuration;
using ZXing.ImageSharp;
using ZXing.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace EUROERP.Infrastructure.NFe;

public interface INfePdfGenerator
{
    Task GeneratePdfAsync(int orderId, string chave, string nfeProcXmlPath, string pdfOutputPath, CancellationToken cancellationToken = default);
}

public class NfePdfGenerator : INfePdfGenerator
{
    private readonly IConfiguration _configuration;
    private const string NsNfe = "http://www.portalfiscal.inf.br/nfe";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Quando true, desenha linhas e rótulos [ Seção N ] entre blocos para facilitar ajustes. Definir false na versão final.</summary>
    private const bool ShowSectionMarkers = true;

    public NfePdfGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task GeneratePdfAsync(int orderId, string chave, string nfeProcXmlPath, string pdfOutputPath, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.EnableDebugging = true;
            var data = LoadDanfeDataFromXml(nfeProcXmlPath, chave);
            if (data == null)
            {
                GenerateFallbackPdf(chave, orderId, pdfOutputPath);
                return;
            }

            var logoPath = _configuration["NFe:DanfeLogoPath"]?.Trim();
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(595, 842); // A4
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Helvetica"));

                    page.Header().Column(_ => { });

                    // ----- Content: canhoto no topo, depois DANFE e resto -----
                    page.Content().Column(content =>
                    {
                        // Seção 1: Canhoto destacável (no content para não estourar limite de altura do header)
                        content.Item().Row(row =>
                        {
                            row.RelativeItem(4).Border(0.5f).Padding(4).Column(col =>
                            {
                                col.Item().DefaultTextStyle(x => x.FontSize(6)).Text(text =>
                                {
                                    text.Span("RECEBEMOS DE ").Bold();
                                    text.Span(data.EmitenteRazaoSocial);
                                    text.Span(" OS PRODUTOS E/OU SERVIÇOS CONSTANTES DA NOTA FISCAL ELETRÔNICA INDICADA AO LADO. ");
                                    text.Span($"EMISSÃO: {data.DhEmi} VALOR TOTAL R$ {data.VNf} DESTINATÁRIO: ");
                                    text.Span(data.DestNome);
                                    text.Span($" {data.DestEndereco} {data.DestBairro} {data.DestMunicipio} - {data.DestUf}");
                                });
                                col.Item().PaddingTop(4).BorderTop(0.5f).PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("DATA DO RECEBIMENTO").Bold().FontSize(6);
                                        c.Item().PaddingTop(1).MinHeight(14);
                                    });
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("IDENTIFICAÇÃO E ASSINATURA DO RECEBEDOR").Bold().FontSize(6);
                                        c.Item().PaddingTop(1).MinHeight(14);
                                    });
                                });
                            });
                            row.RelativeItem(1).Border(0.5f).BorderLeft(0.5f).Padding(4).Column(c =>
                            {
                                c.Item().AlignCenter().Text("NF-e").Bold().FontSize(11);
                                c.Item().PaddingTop(2).AlignCenter().Text($"N° {FormatNfeNumberForCanhoto(data.NNF)}").Bold().FontSize(9);
                                c.Item().AlignCenter().Text($"Série {data.Serie.PadLeft(3, '0')}").Bold().FontSize(9);
                            });
                        });
                        content.Item().PaddingTop(3).AlignCenter().Text(" - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  ").FontSize(6).FontColor(Colors.Grey.Medium);
                        // Seção 2: layout em grid como no modelo (3 colunas em cima, 5 embaixo)
                        content.Item().PaddingTop(6).Column(sec2 =>
                        {
                            // Faixa superior: Emitente | DANFE | Chave de acesso (com espaço para código de barras)
                            sec2.Item().Row(r1 =>
                            {
                                r1.RelativeItem(2).Border(0.5f).Padding(6).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("IDENTIFICAÇÃO DO EMITENTE").Bold().FontSize(6);
                                    col.Item().PaddingTop(7).AlignCenter();
                                    if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                        col.Item().AlignCenter().Width(117).Height(26).Image(logoPath);
                                    else
                                        col.Item().Height(40).Placeholder();
                                    col.Item().PaddingTop(0).AlignCenter().Text(data.EmitenteRazaoSocial).Bold().FontSize(7);
                                    col.Item().AlignCenter().Text(data.EmitenteEndereco).FontSize(6);
                                    col.Item().AlignCenter().Text(data.EmitenteBairroCep).FontSize(6);
                                    col.Item().AlignCenter().Text(data.EmitenteMunicipioUfFone).FontSize(6);
                                });
                                r1.ConstantItem(115).Border(0.5f).Padding(6).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("DANFE").Bold().FontSize(14);
                                    col.Item().AlignCenter().Text("Documento Auxiliar da Nota Fiscal Eletrônica").FontSize(6);
                                    col.Item().PaddingTop(12).AlignCenter().AlignMiddle().Row(r =>
                                    {
                                        r.ConstantItem(72).AlignMiddle().Column(entradaSaida =>
                                        {
                                            entradaSaida.Item().AlignCenter().Text("0 - ENTRADA").FontSize(7);
                                            entradaSaida.Item().AlignCenter().Text("1 - SAÍDA").FontSize(7);
                                        });
                                        r.ConstantItem(6);
                                        r.ConstantItem(18).Border(0.5f).AlignCenter().AlignMiddle().Text(data.TpNF == 0 ? "0" : "1").Bold().FontSize(9);
                                    });
                                    col.Item().PaddingTop(4).AlignCenter().Text($"N° {FormatNfeNumberForCanhoto(data.NNF)}").Bold().FontSize(10);
                                    col.Item().AlignCenter().Text($"Série {data.Serie.PadLeft(3, '0')}  Folha 1/1").FontSize(7);
                                });
                                r1.RelativeItem(3).Border(0.5f).Padding(6).Column(col =>
                                {
                                    col.Item().PaddingTop(12);
                                    var barcodePng = TryGetChaveBarcodePng(chave);
                                    if (barcodePng != null && barcodePng.Length > 0)
                                    {
                                        col.Item().AlignCenter().Width(220).Height(45).Image(barcodePng).FitArea();
                                    }
                                    else
                                    {
                                        col.Item().Height(45).AlignCenter().AlignMiddle();
                                    }
                                    col.Item().PaddingTop(2).AlignCenter().Text("CHAVE DE ACESSO").Bold().FontSize(7);
                                    col.Item().PaddingTop(2).AlignCenter().Text(FormatChaveForDisplay(chave)).Bold().FontSize(8);
                                    col.Item().PaddingTop(4).AlignCenter().Text("Consulta de autenticidade no portal nacional da NF-e").FontSize(5);
                                    col.Item().AlignCenter().Text("www.nfe.fazenda.gov.br/portal ou no site da Sefaz Autorizadora").FontSize(5);
                                });
                            });
                            // Faixa inferior: Natureza | Protocolo | IE | IE Subst + CNPJ
                            sec2.Item().PaddingTop(0).Row(r2 =>
                            {
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("NATUREZA DA OPERAÇÃO").Bold().FontSize(5);
                                    col.Item().PaddingTop(2).AlignCenter().Text(data.NatOp).Bold().FontSize(9);
                                });
                                r2.RelativeItem(2).Border(0.5f).Padding(4).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("PROTOCOLO DE AUTORIZAÇÃO DE USO").Bold().FontSize(5);
                                    col.Item().PaddingTop(2).AlignCenter().Text($"{data.NProt} - {data.DhRecbto}").Bold().FontSize(8);
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("INSCRIÇÃO ESTADUAL").Bold().FontSize(5);
                                    col.Item().PaddingTop(2).AlignCenter().Text(data.EmitenteIE).Bold().FontSize(8);
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("INSCRIÇÃO ESTADUAL DO SUBST. TRIBUT.").Bold().FontSize(5);
                                    col.Item().PaddingTop(2).AlignCenter().Text("").FontSize(8);
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("CNPJ").Bold().FontSize(5);
                                    col.Item().PaddingTop(2).AlignCenter().Text(FormatCnpj(data.EmitenteCnpj)).Bold().FontSize(8);
                                });
                            });
                        });

                        // Destinatário / Remetente — uma caixa por campo, rótulo flutuando acima (como na Seção 2)
                        content.Item().PaddingTop(6).Column(destCol =>
                        {
                            destCol.Item().Text("DESTINATÁRIO / REMETENTE").Bold().FontSize(6);
                            destCol.Item().PaddingTop(2).Row(r1 =>
                            {
                                r1.RelativeItem(2).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("NOME / RAZÃO SOCIAL").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DestNome).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("CNPJ / CPF").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DestCpfCnpj).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("DATA DE EMISSÃO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DhEmi).FontSize(6).Bold();
                                });
                            });
                            destCol.Item().PaddingTop(0).Row(r2 =>
                            {
                                r2.RelativeItem(2).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("ENDEREÇO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DestEndereco).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("BAIRRO / DISTRITO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DestBairro).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("CEP").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DestCep).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("DATA DE SAÍDA").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(DateFromDh(data.DhSaiEnt)).FontSize(6).Bold();
                                });
                            });
                            destCol.Item().PaddingTop(0).Row(r3 =>
                            {
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("MUNICÍPIO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DestMunicipio).FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("UF").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DestUf).FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("FONE / FAX").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(string.IsNullOrEmpty(data.DestFone) ? "" : $"({data.DestFone})").FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("INSCRIÇÃO ESTADUAL").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.DestIE ?? "").FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("HORA DE SAÍDA").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(TimeFromDh(data.DhSaiEnt)).FontSize(6).Bold();
                                });
                            });
                        });

                        // Cálculo do imposto — uma caixa por campo, rótulo à esquerda acima, valor à direita em negrito (como no anexo)
                        content.Item().PaddingTop(6).Column(impostoCol =>
                        {
                            impostoCol.Item().AlignLeft().Text("CÁLCULO DO IMPOSTO").Bold().FontSize(6);
                            impostoCol.Item().PaddingTop(2).Row(r1 =>
                            {
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("BASE DE CÁLCULO DO ICMS").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VBc).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR DO ICMS").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VIcms).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("BASE DE CÁLCULO DO ICMS S.T.").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VBcst).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR DO ICMS SUBSTITUIÇÃO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VSt).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR DO PIS").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VPis).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR TOTAL DOS PRODUTOS").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VProd).FontSize(6).Bold();
                                });
                            });
                            impostoCol.Item().PaddingTop(0).Row(r2 =>
                            {
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR DO FRETE").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VFrete).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR DO SEGURO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VSeg).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("DESCONTO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VDesc).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("OUTRAS DESPESAS").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VOutro).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR TOTAL DO IPI").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VIpi).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR DO COFINS").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VCofins).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("VALOR TOTAL DA NOTA").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VNf).FontSize(6).Bold();
                                });
                            });
                        });

                        // Transportador / Volumes — uma caixa por campo, rótulo à esquerda acima (como no anexo)
                        content.Item().PaddingTop(6).Column(transpCol =>
                        {
                            transpCol.Item().AlignLeft().Text("TRANSPORTADOR / VOLUMES TRANSPORTADOS").Bold().FontSize(6);
                            transpCol.Item().PaddingTop(2).Row(r1 =>
                            {
                                r1.RelativeItem(2).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("NOME / RAZÃO SOCIAL").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspNome).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("FRETE POR CONTA").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspModFreteDesc).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("CÓDIGO ANTT").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspCodAntt).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("PLACA DO VEÍCULO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspPlaca).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("UF").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspPlacaUf).FontSize(6).Bold();
                                });
                                r1.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("CNPJ / CPF").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspCnpjCpf).FontSize(6).Bold();
                                });
                            });
                            transpCol.Item().PaddingTop(0).Row(r2 =>
                            {
                                r2.RelativeItem(2).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("ENDEREÇO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspEndereco).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("MUNICÍPIO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspMunicipio).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("UF").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspUf).FontSize(6).Bold();
                                });
                                r2.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("INSCRIÇÃO ESTADUAL").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.TranspIE).FontSize(6).Bold();
                                });
                            });
                            transpCol.Item().PaddingTop(0).Row(r3 =>
                            {
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("QUANTIDADE").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VolQty).FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("ESPÉCIE").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.VolEsp).FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("MARCA").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.VolMarca).FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("NÚMERO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).Text(data.VolNumero).FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("PESO BRUTO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VolPesoB).FontSize(6).Bold();
                                });
                                r3.RelativeItem(1).Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().AlignLeft().Text("PESO LÍQUIDO").Bold().FontSize(5);
                                    c.Item().PaddingTop(2).AlignRight().Text(data.VolPesoL).FontSize(6).Bold();
                                });
                            });
                        });

                        // Dados dos produtos / serviços — tabela com 14 colunas como no anexo
                        content.Item().PaddingTop(6).Text("DADOS DOS PRODUTOS / SERVIÇOS").Bold().FontSize(6);
                        content.Item().PaddingTop(1).Table(prod =>
                        {
                            prod.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.8f);   // CÓD. PRODUTO (um pouco menor)
                                c.RelativeColumn(3.6f);   // DESCRIÇÃO (mais larga)
                                c.RelativeColumn(0.8f);   // NCM/SH (um pouco menor)
                                c.RelativeColumn(0.65f);  // O/CSOSN
                                c.RelativeColumn(0.5f);   // CFOP (estreita)
                                c.RelativeColumn(0.35f); // UN (estreita)
                                c.RelativeColumn(0.6f);   // QUANT
                                c.RelativeColumn(0.75f);  // VALOR UNIT
                                c.RelativeColumn(0.9f);   // VALOR TOTAL
                                c.RelativeColumn(0.7f);   // B.CALC / ICMS
                                c.RelativeColumn(0.55f);  // VALOR ICMS (estreita)
                                c.RelativeColumn(0.55f);  // VALOR IPI (estreita)
                                c.RelativeColumn(0.7f);   // ALÍQ. ICMS
                                c.RelativeColumn(0.5f);   // ALÍQ. IPI (estreita)
                            });
                            prod.Header(h =>
                            {
                                h.Cell().Element(CellBorder).Text("CÓD. PRODUTO").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("DESCRIÇÃO DO PRODUTO / SERVIÇO").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("NCM/SH").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("O/CSOSN").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("CFOP").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("UN").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("QUANT").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("VALOR UNIT").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("VALOR TOTAL").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Column(cab => { cab.Item().Text("B.CALC").FontSize(5).Bold(); cab.Item().Text("ICMS").FontSize(5).Bold(); });
                                h.Cell().Element(CellBorder).Text("VALOR ICMS").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("VALOR IPI").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("ALÍQ. ICMS").FontSize(5).Bold();
                                h.Cell().Element(CellBorder).Text("ALÍQ. IPI").FontSize(5).Bold();
                            });
                            foreach (var item in data.Produtos)
                            {
                                prod.Cell().Element(CellBorder).AlignRight().Text(item.CProd).FontSize(6);
                                prod.Cell().Element(CellBorder).Text(item.XProd).FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text(item.Ncm).FontSize(6);
                                prod.Cell().Element(CellBorder).Text(item.OrigemCsosn).FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text(item.Cfop).FontSize(6);
                                prod.Cell().Element(CellBorder).Text(item.UCom).FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text(FormatDecimalPtBr(item.QCom)).FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text(FormatDecimalPtBr(item.VUnCom)).FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text(FormatDecimalPtBr(item.VProd)).FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text("0,00").FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text("0,00").FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text("").FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text(FormatDecimalPtBr(item.PIcms)).FontSize(6);
                                prod.Cell().Element(CellBorder).AlignRight().Text("").FontSize(6);
                            }
                        });

                        // Seção 8: DADOS ADICIONAIS — título no canto; duas colunas separadas apenas por linha vertical
                        var infCompleta = BuildInformacoesComplementares(data);
                        content.Item().PaddingTop(6).Column(sec8 =>
                        {
                            sec8.Item().Text("DADOS ADICIONAIS").Bold().FontSize(6);
                            sec8.Item().PaddingTop(3).Border(0.5f).Row(r =>
                            {
                                r.RelativeItem(3).BorderRight(0.5f).Padding(4).Column(col =>
                                {
                                    col.Item().Text("INFORMAÇÕES COMPLEMENTARES").Bold().FontSize(6);
                                    if (!string.IsNullOrEmpty(infCompleta))
                                    {
                                        foreach (var line in infCompleta.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                                        {
                                            if (!string.IsNullOrWhiteSpace(line))
                                                col.Item().PaddingTop(1).Text(line.Trim()).FontSize(6);
                                        }
                                    }
                                });
                                r.RelativeItem(1).Padding(4).Column(col => col.Item().Text("RESERVADO AO FISCO").Bold().FontSize(6));
                            });
                        });
                    });

                    // Footer removido — o texto "RECEBEMOS..." já está no canhoto no início do PDF
                    page.Footer().Column(_ => { });
                });
            });

            doc.GeneratePdf(pdfOutputPath);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static IContainer CellBorder(IContainer c) => c.Border(0.5f).Padding(3);

    private static IContainer SectionSeparator(IContainer c, int sectionNumber)
    {
        if (!ShowSectionMarkers) return c;
        c.Column(col =>
        {
            col.Item().PaddingTop(6).BorderBottom(1.5f).BorderColor(Colors.Blue.Medium).PaddingBottom(4);
            col.Item().AlignCenter().Text($"[ Seção {sectionNumber} ]").FontSize(7).Bold().FontColor(Colors.Blue.Medium);
        });
        return c;
    }

    /// <summary>Monta o texto do subbloco Informações Complementares: mensagem F2 (InfAdic), linhas do appsettings e valor aproximado dos impostos (como no legado: vNF * ICMS_ALIQ / 100).</summary>
    private string BuildInformacoesComplementares(DanfeData data)
    {
        var sb = new StringBuilder();
        // Mensagem na nota (F2) — vem do XML infCpl quando foi autorizada
        if (!string.IsNullOrWhiteSpace(data.InfAdic))
            sb.AppendLine(data.InfAdic.Trim());

        // Para NF-e de Saída (tpNF == 1): texto padrão Simples Nacional + valor aprox. impostos + linhas fixas (como no legado)
        if (data.TpNF == 1)
        {
            var docSimples = _configuration["NFe:DadosAdicionais:DocumentoSimplesNacional"] ?? "Documento emitido por ME ou EPP optante pelo Simples Nacional.";
            sb.AppendLine(docSimples);

            var icmsAliqStr = _configuration["NFe:ICMS_ALIQ"]?.Trim().Replace(',', '.');
            if (!string.IsNullOrEmpty(icmsAliqStr) && decimal.TryParse(icmsAliqStr, System.Globalization.NumberStyles.Number, Inv, out var aliq))
            {
                var vNfStr = (data.VNf ?? "").Trim().Replace(',', '.');
                if (decimal.TryParse(vNfStr, System.Globalization.NumberStyles.Number, Inv, out var vNf))
                {
                    var imposto = Math.Round(vNf * aliq / 100m, 2);
                    sb.AppendLine("Valor aproximados dos impostos: R$ " + FormatDecimalPtBr(imposto.ToString("F2", Inv)));
                }
            }

            var registroMpa = _configuration["NFe:DadosAdicionais:RegistroMpa"] ?? "REGISTRO MPA - RGP - 1104-SP";
            sb.AppendLine(registroMpa);
            var envioPeixes = _configuration["NFe:DadosAdicionais:EnvioPeixes"] ?? "Envio de peixes ornamentais sem a necessidade de Guia de Transporte de Animais - GTA";
            sb.AppendLine(envioPeixes);
            var portaria = _configuration["NFe:DadosAdicionais:PortariaSapMapa"] ?? "De acordo com Art 9o da Portaria SAP/MAPA No 17, de 26 Jan de 2021";
            sb.AppendLine(portaria);
        }

        return sb.ToString().TrimEnd();
    }

    private void GenerateFallbackPdf(string chave, int orderId, string pdfOutputPath)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(595, 842);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("DOCUMENTO AUXILIAR DA NOTA FISCAL ELETRÔNICA").Bold().FontSize(12);
                    col.Item().PaddingTop(8).Text(t => { t.Span("Chave de acesso: ").Bold(); t.Span(chave); });
                    col.Item().PaddingTop(4).Text(FormatChaveForDisplay(chave)).FontSize(8);
                });
                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().Text($"Pedido: {orderId}");
                    col.Item().PaddingTop(8).Text("Consulte a chave de acesso em https://portalsped.fazenda.mg.gov.br/ ou no site da SEFAZ do seu estado.");
                });
                page.Footer().AlignCenter().Text(t => { t.Span("Página "); t.CurrentPageNumber(); });
            });
        });
        doc.GeneratePdf(pdfOutputPath);
    }

    private static DanfeData? LoadDanfeDataFromXml(string nfeProcXmlPath, string chave)
    {
        if (!File.Exists(nfeProcXmlPath)) return null;
        var doc = new XmlDocument();
        try
        {
            doc.Load(nfeProcXmlPath);
        }
        catch
        {
            return null;
        }

        var nfe = doc.DocumentElement?.SelectSingleNode("*[local-name()='NFe']") as XmlElement;
        var infNFe = nfe?.SelectSingleNode("*[local-name()='infNFe']") as XmlElement;
        var protNFe = doc.DocumentElement?.SelectSingleNode("*[local-name()='protNFe']") as XmlElement;
        var infProt = protNFe?.SelectSingleNode("*[local-name()='infProt']") as XmlElement;
        if (infNFe == null) return null;

        string Get(XmlNode? p, string name)
        {
            if (p == null) return "";
            var el = p.SelectSingleNode($"*[local-name()='{name}']");
            return el?.InnerText?.Trim() ?? "";
        }

        var ide = infNFe.SelectSingleNode("*[local-name()='ide']") as XmlElement;
        var emit = infNFe.SelectSingleNode("*[local-name()='emit']") as XmlElement;
        var enderEmit = emit?.SelectSingleNode("*[local-name()='enderEmit']") as XmlElement;
        var dest = infNFe.SelectSingleNode("*[local-name()='dest']") as XmlElement;
        var enderDest = dest?.SelectSingleNode("*[local-name()='enderDest']") as XmlElement;
        var total = infNFe.SelectSingleNode("*[local-name()='total']") as XmlElement;
        var icmsTot = total?.SelectSingleNode("*[local-name()='ICMSTot']") as XmlElement;
        var transp = infNFe.SelectSingleNode("*[local-name()='transp']") as XmlElement;
        var infAdic = infNFe.SelectSingleNode("*[local-name()='infAdic']") as XmlElement;

        var detList = infNFe.SelectNodes("*[local-name()='det']");
        var produtos = new List<DanfeProduto>();
        if (detList != null)
        {
            foreach (XmlNode det in detList)
            {
                var detEl = det as XmlElement;
                var prod = detEl?.SelectSingleNode("*[local-name()='prod']") as XmlElement;
                if (prod == null) continue;
                var imposto = detEl?.SelectSingleNode("*[local-name()='imposto']") as XmlElement;
                var icms = imposto?.SelectSingleNode("*[local-name()='ICMS']/*") as XmlElement;
                var orig = icms != null ? Get(icms, "orig") : "";
                var csosn = icms != null ? Get(icms, "CSOSN") : "";
                var origemCsosn = string.IsNullOrEmpty(csosn) ? "" : $"{orig}/{csosn}";
                var ipiNode = imposto?.SelectSingleNode("*[local-name()='IPI']/*") as XmlElement;
                produtos.Add(new DanfeProduto
                {
                    CProd = Get(prod, "cProd"),
                    XProd = Get(prod, "xProd"),
                    Ncm = Get(prod, "NCM"),
                    Cfop = Get(prod, "CFOP"),
                    UCom = Get(prod, "uCom"),
                    QCom = Get(prod, "qCom"),
                    VUnCom = Get(prod, "vUnCom"),
                    VProd = Get(prod, "vProd"),
                    OrigemCsosn = origemCsosn,
                    VBcIcms = icms != null ? Get(icms, "vBC") : "",
                    VIcms = icms != null ? Get(icms, "vICMS") : "",
                    PIcms = icms != null ? Get(icms, "pICMS") : "",
                    VIpi = ipiNode != null ? Get(ipiNode, "vIPI") : "",
                    PIpi = ipiNode != null ? Get(ipiNode, "pIPI") : ""
                });
            }
        }

        var tpNF = int.TryParse(Get(ide, "tpNF"), out var t) ? t : 1;
        var nNF = Get(ide, "nNF");
        var serie = Get(ide, "serie");
        var natOp = Get(ide, "natOp");
        var dhEmi = ParseDh(Get(ide, "dhEmi"));
        var dhSaiEnt = ParseDh(Get(ide, "dhSaiEnt"));
        var nProt = Get(infProt, "nProt");
        var dhRecbto = ParseDh(Get(infProt, "dhRecbto"));

        var emitCnpj = Get(emit, "CNPJ");
        var emitNome = Get(emit, "xNome");
        var emitIE = Get(emit, "IE");
        var emitLgr = Get(enderEmit, "xLgr");
        var emitNro = Get(enderEmit, "nro");
        var emitCpl = Get(enderEmit, "xCpl");
        var emitBairro = Get(enderEmit, "xBairro");
        var emitCep = Get(enderEmit, "CEP");
        var emitMun = Get(enderEmit, "xMun");
        var emitUf = Get(enderEmit, "UF");
        var emitFone = Get(enderEmit, "fone");

        var destCpfCnpj = Get(dest, "CPF");
        if (string.IsNullOrEmpty(destCpfCnpj)) destCpfCnpj = Get(dest, "CNPJ");
        var destNome = Get(dest, "xNome");
        var destLgr = Get(enderDest, "xLgr");
        var destNro = Get(enderDest, "nro");
        var destCpl = Get(enderDest, "xCpl");
        var destBairro = Get(enderDest, "xBairro");
        var destCep = Get(enderDest, "CEP");
        var destMun = Get(enderDest, "xMun");
        var destUf = Get(enderDest, "UF");
        var destFone = Get(enderDest, "fone");
        var destIE = Get(dest, "IE");

        var vBc = Get(icmsTot, "vBC");
        var vIcms = Get(icmsTot, "vICMS");
        var vBcst = Get(icmsTot, "vBCST");
        var vSt = Get(icmsTot, "vST");
        var vProd = Get(icmsTot, "vProd");
        var vFrete = Get(icmsTot, "vFrete");
        var vSeg = Get(icmsTot, "vSeg");
        var vDesc = Get(icmsTot, "vDesc");
        var vOutro = Get(icmsTot, "vOutro");
        var vIpi = Get(icmsTot, "vIPI");
        var vPis = Get(icmsTot, "vPIS");
        var vCofins = Get(icmsTot, "vCOFINS");
        var vNf = Get(icmsTot, "vNF");

        var modFrete = Get(transp, "modFrete");
        var transporta = transp?.SelectSingleNode("*[local-name()='transporta']") as XmlElement;
        var veiculo = transp?.SelectSingleNode("*[local-name()='veiculo']") as XmlElement;
        var vol = transp?.SelectSingleNode("*[local-name()='vol']") as XmlElement;
        var transpNome = transporta != null ? Get(transporta, "xNome") : "";
        var transpCnpj = transporta != null ? Get(transporta, "CNPJ") : "";
        var transpCpf = transporta != null ? Get(transporta, "CPF") : "";
        var transpCnpjCpf = !string.IsNullOrEmpty(transpCnpj) ? FormatCnpj(transpCnpj) : (!string.IsNullOrEmpty(transpCpf) ? FormatCpfCnpj(transpCpf) : "");
        var transpEndereco = transporta != null ? Get(transporta, "xEnder") : "";
        var transpMunicipio = transporta != null ? Get(transporta, "xMun") : "";
        var transpUf = transporta != null ? Get(transporta, "UF") : "";
        var transpIE = transporta != null ? Get(transporta, "IE") : "";
        var transpCodAntt = transp != null ? Get(transp, "cAntt") : ""; if (string.IsNullOrEmpty(transpCodAntt) && transporta != null) transpCodAntt = Get(transporta, "ANTT");
        var transpPlaca = veiculo != null ? Get(veiculo, "placa") : "";
        var transpPlacaUf = veiculo != null ? Get(veiculo, "UF") : "";
        var volQty = vol != null ? Get(vol, "qVol") : "";
        var volEsp = vol != null ? Get(vol, "esp") : "";
        var volMarca = vol != null ? Get(vol, "marca") : "";
        var volNumero = vol != null ? Get(vol, "nVol") : "";
        var pesoB = vol != null ? Get(vol, "pesoB") : "";
        var pesoL = vol != null ? Get(vol, "pesoL") : "";
        var modFreteDesc = modFrete == "0" ? "(0) Emitente" : modFrete == "1" ? "(1) Destinatário" : "(9) Sem frete";
        var transpVolLine = string.IsNullOrEmpty(volQty) && string.IsNullOrEmpty(volEsp) ? "" : $"{volQty ?? ""} {volEsp ?? ""} {pesoB ?? ""} {pesoL ?? ""}".Trim();

        var emitEndereco = $"{emitLgr}, {emitNro}".TrimEnd(' ', ',');
        if (!string.IsNullOrEmpty(emitCpl)) emitEndereco += " " + emitCpl;
        var emitBairroCep = $"{emitBairro} - CEP {FormatCep(emitCep)}";
        var emitMunicipioUfFone = $"{emitMun}/{emitUf} Telefones: {emitFone}".TrimEnd(' ', '/', ':');

        var destEndereco = $"{destLgr}, {destNro}".TrimEnd(' ', ',');
        if (!string.IsNullOrEmpty(destCpl)) destEndereco += " " + destCpl;

        var infAdicText = Get(infAdic, "infCpl");
        if (string.IsNullOrEmpty(infAdicText)) infAdicText = Get(infAdic, "infAdFisco");

        return new DanfeData
        {
            TpNF = tpNF,
            NNF = nNF,
            Serie = serie,
            NatOp = natOp,
            DhEmi = dhEmi,
            DhSaiEnt = dhSaiEnt,
            NProt = nProt,
            DhRecbto = dhRecbto,
            EmitenteCnpj = emitCnpj,
            EmitenteRazaoSocial = emitNome,
            EmitenteIE = emitIE,
            EmitenteEndereco = emitEndereco,
            EmitenteBairroCep = emitBairroCep,
            EmitenteMunicipioUfFone = emitMunicipioUfFone,
            DestCpfCnpj = FormatCpfCnpj(destCpfCnpj),
            DestNome = destNome,
            DestEndereco = destEndereco,
            DestBairro = destBairro,
            DestCep = FormatCep(destCep),
            DestMunicipio = destMun,
            DestUf = destUf,
            DestFone = destFone,
            DestIE = destIE,
            VBc = vBc,
            VIcms = vIcms,
            VBcst = vBcst,
            VSt = vSt,
            VProd = vProd,
            VFrete = vFrete,
            VSeg = vSeg,
            VDesc = vDesc,
            VOutro = vOutro,
            VIpi = vIpi,
            VPis = vPis,
            VCofins = vCofins,
            VNf = vNf,
            TranspModFreteDesc = modFreteDesc,
            TranspNome = transpNome,
            TranspCnpjCpf = transpCnpjCpf,
            TranspEndereco = transpEndereco,
            TranspMunicipio = transpMunicipio,
            TranspUf = transpUf,
            TranspIE = transpIE,
            TranspCodAntt = transpCodAntt,
            TranspPlaca = transpPlaca,
            TranspPlacaUf = transpPlacaUf,
            VolQty = volQty,
            VolEsp = volEsp,
            VolMarca = volMarca,
            VolNumero = volNumero,
            VolPesoB = pesoB,
            VolPesoL = pesoL,
            TranspVolLine = transpVolLine,
            Produtos = produtos,
            InfAdic = infAdicText
        };
    }

    private static string ParseDh(string dh)
    {
        if (string.IsNullOrEmpty(dh)) return "";
        if (DateTime.TryParse(dh, Inv, System.Globalization.DateTimeStyles.None, out var dt))
            return dt.ToString("dd/MM/yyyy HH:mm:ss", Inv);
        return dh;
    }

    private static string DateFromDh(string dh)
    {
        if (string.IsNullOrEmpty(dh)) return "";
        var i = dh.IndexOf(' ');
        return i > 0 ? dh.Substring(0, i) : dh;
    }

    private static string TimeFromDh(string dh)
    {
        if (string.IsNullOrEmpty(dh)) return "";
        var i = dh.IndexOf(' ');
        return i >= 0 && i + 1 < dh.Length ? dh.Substring(i + 1) : "";
    }

    private static string FormatCnpj(string cnpj)
    {
        var d = NfeChaveHelper.CleanDigits(cnpj);
        if (d.Length != 14) return cnpj;
        return $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d.Substring(12, 2)}";
    }

    private static string FormatCpfCnpj(string doc)
    {
        var d = NfeChaveHelper.CleanDigits(doc);
        if (d.Length == 11) return $"{d.Substring(0, 3)}.{d.Substring(3, 3)}.{d.Substring(6, 3)}-{d.Substring(9, 2)}";
        if (d.Length == 14) return FormatCnpj(doc);
        return doc;
    }

    private static string FormatCep(string cep)
    {
        var d = NfeChaveHelper.CleanDigits(cep);
        if (d.Length != 8) return cep;
        return $"{d.Substring(0, 5)}-{d.Substring(5, 3)}";
    }

    private static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>Formata valor numérico para pt-BR (vírgula decimal). Ex.: "12.40" -> "12,40".</summary>
    private static string FormatDecimalPtBr(string? value, int decimals = 2)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? "";
        var normalized = value.Trim().Replace(',', '.');
        if (decimal.TryParse(normalized, System.Globalization.NumberStyles.Number, Inv, out var num))
            return num.ToString($"N{decimals}", PtBr);
        return value;
    }

    private static string FormatNfeNumberForCanhoto(string nNF)
    {
        var d = (nNF ?? "").Trim();
        if (string.IsNullOrEmpty(d) || !int.TryParse(d, System.Globalization.NumberStyles.None, Inv, out var num))
            return nNF ?? "";
        var s = num.ToString("D9", Inv);
        return $"{s.Substring(0, 3)}.{s.Substring(3, 3)}.{s.Substring(6, 3)}";
    }

    private static string FormatChaveForDisplay(string chave)
    {
        if (string.IsNullOrEmpty(chave) || chave.Length != 44) return chave;
        var sb = new StringBuilder();
        for (int i = 0; i < 44; i++)
        {
            if (i > 0 && i % 4 == 0) sb.Append(' ');
            sb.Append(chave[i]);
        }
        return sb.ToString();
    }

    /// <summary>Gera código de barras Code 128 da chave (44 dígitos) em PNG. Retorna null se chave inválida ou erro.</summary>
    private static byte[]? TryGetChaveBarcodePng(string chave)
    {
        if (string.IsNullOrEmpty(chave)) return null;
        var digits = new string(chave.Where(char.IsDigit).ToArray());
        if (digits.Length != 44) return null;
        try
        {
            var writer = new BarcodeWriter<Rgba32>
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 320,
                    Height = 50,
                    Margin = 2,
                    PureBarcode = false
                }
            };
            using var image = writer.Write(digits);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private sealed class DanfeData
    {
        public int TpNF { get; set; }
        public string NNF { get; set; } = "";
        public string Serie { get; set; } = "";
        public string NatOp { get; set; } = "";
        public string DhEmi { get; set; } = "";
        public string DhSaiEnt { get; set; } = "";
        public string NProt { get; set; } = "";
        public string DhRecbto { get; set; } = "";
        public string EmitenteCnpj { get; set; } = "";
        public string EmitenteRazaoSocial { get; set; } = "";
        public string EmitenteIE { get; set; } = "";
        public string EmitenteEndereco { get; set; } = "";
        public string EmitenteBairroCep { get; set; } = "";
        public string EmitenteMunicipioUfFone { get; set; } = "";
        public string DestCpfCnpj { get; set; } = "";
        public string DestNome { get; set; } = "";
        public string DestEndereco { get; set; } = "";
        public string DestBairro { get; set; } = "";
        public string DestCep { get; set; } = "";
        public string DestMunicipio { get; set; } = "";
        public string DestUf { get; set; } = "";
        public string DestFone { get; set; } = "";
        public string DestIE { get; set; } = "";
        public string VBc { get; set; } = "";
        public string VIcms { get; set; } = "";
        public string VBcst { get; set; } = "";
        public string VSt { get; set; } = "";
        public string VProd { get; set; } = "";
        public string VFrete { get; set; } = "";
        public string VSeg { get; set; } = "";
        public string VDesc { get; set; } = "";
        public string VOutro { get; set; } = "";
        public string VIpi { get; set; } = "";
        public string VPis { get; set; } = "";
        public string VCofins { get; set; } = "";
        public string VNf { get; set; } = "";
        public string TranspModFreteDesc { get; set; } = "";
        public string TranspNome { get; set; } = "";
        public string TranspCnpjCpf { get; set; } = "";
        public string TranspEndereco { get; set; } = "";
        public string TranspMunicipio { get; set; } = "";
        public string TranspUf { get; set; } = "";
        public string TranspIE { get; set; } = "";
        public string TranspCodAntt { get; set; } = "";
        public string TranspPlaca { get; set; } = "";
        public string TranspPlacaUf { get; set; } = "";
        public string VolQty { get; set; } = "";
        public string VolEsp { get; set; } = "";
        public string VolMarca { get; set; } = "";
        public string VolNumero { get; set; } = "";
        public string VolPesoB { get; set; } = "";
        public string VolPesoL { get; set; } = "";
        public string TranspVolLine { get; set; } = "";
        public List<DanfeProduto> Produtos { get; set; } = new();
        public string InfAdic { get; set; } = "";
    }

    private sealed class DanfeProduto
    {
        public string CProd { get; set; } = "";
        public string XProd { get; set; } = "";
        public string Ncm { get; set; } = "";
        public string Cfop { get; set; } = "";
        public string UCom { get; set; } = "";
        public string QCom { get; set; } = "";
        public string VUnCom { get; set; } = "";
        public string VProd { get; set; } = "";
        public string OrigemCsosn { get; set; } = "";
        public string VBcIcms { get; set; } = "";
        public string VIcms { get; set; } = "";
        public string VIpi { get; set; } = "";
        public string PIcms { get; set; } = "";
        public string PIpi { get; set; } = "";
    }
}
