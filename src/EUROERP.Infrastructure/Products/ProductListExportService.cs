using System.Data;
using Dapper;
using EUROERP.Application.Products;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EUROERP.Infrastructure.Products;

public class ProductListExportService : IProductListExportService
{
    private readonly IDbConnection _connection;
    private readonly IProductReferenceService _referenceService;

    /// <summary>Eurobus uses product class 1 (not Aquanimal class 4).</summary>
    private const byte EurobusProductClassId = 1;
    private const byte DefaultMarketId = 1;

    public ProductListExportService(IDbConnection connection, IProductReferenceService referenceService)
    {
        _connection = connection;
        _referenceService = referenceService;
    }

    public async Task<ProductListExportDto> GetProductListDataAsync(ProductListRequest request, CancellationToken cancellationToken = default)
    {
        var title = request.Mode == ProductListMode.Stock ? "Lista de Saldos" : "Lista de Preços";
        var result = new ProductListExportDto
        {
            Title = title,
            Date = DateTime.Today,
            Mode = request.Mode
        };

        var groups = await ResolveGroupsAsync(request, cancellationToken);
        var onlyInStock = request.Mode == ProductListMode.PriceInStock;

        foreach (var g in groups)
        {
            var items = await GetProductsForListAsync(DefaultMarketId, (byte)g.Id, onlyInStock, cancellationToken);
            if (items.Count == 0)
                continue;

            result.Groups.Add(new ProductListGroupDto
            {
                GroupId = (byte)g.Id,
                GroupName = g.Name,
                Items = items
            });
        }

        return result;
    }

    public async Task<byte[]> GeneratePdfAsync(ProductListRequest request, CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var data = await GetProductListDataAsync(request, cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateExcelAsync(ProductListRequest request, CancellationToken cancellationToken = default)
    {
        var data = await GetProductListDataAsync(request, cancellationToken);

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Lista");

        var row = 1;
        ws.Cell(row, 1).Value = data.Title;
        ws.Row(row).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = $"Data: {data.Date:dd/MM/yyyy}";
        row += 2;

        var isStock = data.Mode == ProductListMode.Stock;

        foreach (var group in data.Groups)
        {
            ws.Cell(row, 1).Value = group.GroupName;
            ws.Row(row).Style.Font.Bold = true;
            row++;

            ws.Cell(row, 1).Value = "Código";
            ws.Cell(row, 2).Value = "Cod. Forn.";
            ws.Cell(row, 3).Value = "Nome";
            if (!isStock)
            {
                ws.Cell(row, 4).Value = "Descrição";
                ws.Cell(row, 5).Value = "Preço";
            }
            else
            {
                ws.Cell(row, 4).Value = "Saldo";
                ws.Cell(row, 5).Value = "Unidade";
            }
            ws.Row(row).Style.Font.Bold = true;
            row++;

            foreach (var item in group.Items)
            {
                ws.Cell(row, 1).Value = item.LionCode;
                ws.Cell(row, 2).Value = item.ExternalPkid ?? "";
                ws.Cell(row, 3).Value = item.Name;
                if (!isStock)
                {
                    ws.Cell(row, 4).Value = item.Description ?? "";
                    ws.Cell(row, 5).Value = item.Price;
                }
                else
                {
                    ws.Cell(row, 4).Value = item.Stock;
                    ws.Cell(row, 5).Value = item.UnitLabel ?? "";
                }
                row++;
            }
            row += 2;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream, false);
        return stream.ToArray();
    }

    private async Task<IReadOnlyList<IdNameDto>> ResolveGroupsAsync(ProductListRequest request, CancellationToken cancellationToken)
    {
        var allGroups = await _referenceService.GetProductGroupsByClassAsync(EurobusProductClassId, cancellationToken);
        if (request.AllGroups || request.GroupIds.Count == 0)
            return allGroups;

        var selected = request.GroupIds.ToHashSet();
        return allGroups.Where(g => selected.Contains((byte)g.Id)).OrderBy(g => g.Name).ToList();
    }

    private async Task<List<ProductListItemDto>> GetProductsForListAsync(byte marketId, byte groupId, bool onlyInStock, CancellationToken cancellationToken)
    {
        // Eurobus ProductController.getProductsForList — excludeQuarantined=false on list results page.
        var sql = @"
            SELECT
                p.PKId AS Id,
                CAST(pg.PRODUCT_CLASS_ID AS varchar) + RIGHT('000000' + CAST(p.PKId AS varchar), 7) AS LionCode,
                p.EXTERNAL_PKID AS ExternalPkid,
                p.NAME AS Name,
                p.DESCRIPTION AS Description,
                ROUND(mp.PRICE * ISNULL(cc.CONVERSION, 1), 2) AS Price,
                p.STOCK AS Stock,
                un.LABEL AS UnitLabel,
                CASE WHEN un.DECIMAL_IND = 1 THEN 1 ELSE 0 END AS DecimalStock
            FROM PRODUCT p
            JOIN UNITS un ON un.PKId = p.UNIT_ID
            JOIN PRODUCT_GROUP pg ON p.GROUP_ID = pg.PKId
            JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId
            JOIN MARKET m ON m.PKId = mp.MARKET_ID
            JOIN CURRENCY_CONVERSION cc ON cc.SOURCE_CURRENCY_ID = m.CURRENCY_ID AND cc.TARGET_CURRENCY_ID = p.CURRENCY_ID
            WHERE pg.PKId = @GroupId
              AND p.ACTIVE = 'Y'
              AND m.PKId = @MarketId";

        if (onlyInStock)
            sql += " AND p.STOCK > 0";

        sql += " ORDER BY p.NAME";

        var list = await _connection.QueryAsync<ProductListItemDto>(
            new CommandDefinition(sql, new { MarketId = marketId, GroupId = groupId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    private static void ComposeHeader(IContainer container, ProductListExportDto data)
    {
        container.Column(column =>
        {
            column.Item().Text(data.Title).Bold().FontSize(14);
            column.Item().Text($"Data: {data.Date:dd/MM/yyyy}").FontSize(9);
            column.Item().Height(8);
        });
    }

    private static void ComposeContent(IContainer container, ProductListExportDto data)
    {
        var isStock = data.Mode == ProductListMode.Stock;

        container.Column(column =>
        {
            foreach (var group in data.Groups)
            {
                column.Item().PaddingBottom(4).Text(group.GroupName).Bold().FontSize(10);
                column.Item().Table(table =>
                {
                    if (isStock)
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(35);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Código");
                            header.Cell().Element(CellStyle).Text("Nome");
                            header.Cell().Element(CellStyle).AlignRight().Text("Saldo");
                            header.Cell().Element(CellStyle).Text("Un.");
                        });
                        foreach (var item in group.Items)
                        {
                            table.Cell().Element(CellStyle).Text(FormatCode(item));
                            table.Cell().Element(CellStyle).Text(Truncate(item.Name, 70));
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatStock(item));
                            table.Cell().Element(CellStyle).Text(item.UnitLabel ?? "");
                        }
                    }
                    else
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(50);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Código");
                            header.Cell().Element(CellStyle).Text("Nome / Descrição");
                            header.Cell().Element(CellStyle).AlignRight().Text("Preço");
                        });
                        foreach (var item in group.Items)
                        {
                            var desc = string.IsNullOrWhiteSpace(item.Description) ? "" : $"\n{item.Description}";
                            table.Cell().Element(CellStyle).Text(FormatCode(item));
                            table.Cell().Element(CellStyle).Text(Truncate(item.Name + desc, 120));
                            table.Cell().Element(CellStyle).AlignRight().Text(item.Price.ToString("N2"));
                        }
                    }
                });
                column.Item().Height(12);
            }
        });
    }

    private static string FormatCode(ProductListItemDto item)
    {
        var ext = string.IsNullOrWhiteSpace(item.ExternalPkid) ? "" : $" - {item.ExternalPkid}";
        return item.LionCode + ext;
    }

    private static string FormatStock(ProductListItemDto item) =>
        item.DecimalStock ? item.Stock.ToString("N3") : item.Stock.ToString("N0");

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static IContainer CellStyle(IContainer c) =>
        c.DefaultTextStyle(s => s.FontSize(8)).Padding(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
}
