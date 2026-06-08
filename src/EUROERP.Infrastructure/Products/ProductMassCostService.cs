using System.Data;
using Dapper;
using EUROERP.Application.Products;

namespace EUROERP.Infrastructure.Products;

public class ProductMassCostService : IProductMassCostService
{
    private readonly IDbConnection _connection;

    public ProductMassCostService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<ProductMassCostItemDto>> GetListForMassCostAsync(ProductFilterDto filter, CancellationToken cancellationToken = default)
    {
        var marketId = filter.MarketId == 0 ? (byte)1 : filter.MarketId;
        var hasParam = filter.ClassId > 0 || filter.GroupId > 0 ||
                       (!string.IsNullOrWhiteSpace(filter.Name) && filter.Name.Length >= 2) ||
                       (!string.IsNullOrWhiteSpace(filter.SciName) && filter.SciName.Length >= 2) ||
                       filter.Code > 0 || filter.SupplierId > 0;
        if (!hasParam)
            return new List<ProductMassCostItemDto>();

        var sql = @"
            SELECT DISTINCT
                p.PKId,
                p.NAME AS Name,
                p.SCI_NAME AS SciName,
                az.NAME AS Size,
                cur.SYMBOL AS Symbol,
                p.COST_GROSS AS CostGross,
                ISNULL(p.DISCOUNT, 0) AS Discount,
                p.COST_NET AS CostNet,
                p.COST_TRANSPORT AS CostTransport,
                p.COST_FINAL AS CostFinal,
                ISNULL(mp.PROFIT, 0) AS Profit,
                mp.PRICE AS Price,
                curMkt.SYMBOL AS SymbolMkt,
                ISNULL(cv.CONVERSION, 1) AS Conversion,
                ROUND(mp.PRICE * ISNULL(cv.CONVERSION, 1), 4) AS PriceMarket,
                p.STOCK AS Stock
            FROM PRODUCT p
            JOIN PRODUCT_GROUP pg ON p.GROUP_ID = pg.PKId
            JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId AND mp.MARKET_ID = @MarketId
            JOIN MARKET mkt ON mkt.PKId = mp.MARKET_ID
            JOIN CURRENCY cur ON cur.PKId = p.CURRENCY_ID
            JOIN CURRENCY curMkt ON curMkt.PKId = mkt.CURRENCY_ID
            LEFT JOIN ANIMAL_SIZE az ON az.PKId = p.SIZE_ID
            LEFT JOIN CURRENCY_CONVERSION cv ON cv.TARGET_CURRENCY_ID = p.CURRENCY_ID AND cv.SOURCE_CURRENCY_ID = mkt.CURRENCY_ID
            LEFT JOIN PRODUCT_SUPPLIER_LINK psl ON psl.PRODUCT_ID = p.PKId
            WHERE 1=1";

        if (!filter.IncludeInactive)
            sql += " AND p.ACTIVE = 'Y'";
        else
            sql += " AND p.ACTIVE IN ('Y','N')";
        if (filter.ClassId > 0)
            sql += " AND pg.PRODUCT_CLASS_ID = @ClassId";
        if (filter.GroupId > 0)
            sql += " AND p.GROUP_ID = @GroupId";
        if (!string.IsNullOrWhiteSpace(filter.Name) && filter.Name.Length >= 2)
            sql += " AND p.NAME LIKE @NameLike";
        if (!string.IsNullOrWhiteSpace(filter.SciName) && filter.SciName.Length >= 2)
            sql += " AND p.SCI_NAME LIKE @SciNameLike";
        if (filter.Code > 0)
            sql += " AND p.PKId = @Code";
        if (filter.SupplierId > 0)
            sql += " AND psl.SUPPLIER_ID = @SupplierId";

        sql += " ORDER BY p.NAME";

        var nameLike = string.IsNullOrWhiteSpace(filter.Name) || filter.Name.Length < 2 ? null : $"%{filter.Name.Trim()}%";
        var sciNameLike = string.IsNullOrWhiteSpace(filter.SciName) || filter.SciName.Length < 2 ? null : $"%{filter.SciName.Trim()}%";

        var list = await _connection.QueryAsync<ProductMassCostItemDto>(
            new CommandDefinition(sql, new
            {
                MarketId = marketId,
                filter.ClassId,
                filter.GroupId,
                NameLike = nameLike,
                SciNameLike = sciNameLike,
                filter.Code,
                filter.SupplierId
            }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<(bool Success, string Message)> UpdateRowAsync(ProductMassCostUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.CostGross < 0 || dto.CostGross > 1000000)
            return (false, "Custo bruto deve estar entre 0 e 1000000.");
        if (dto.Discount < 0 || dto.Discount > 99)
            return (false, "Desconto % deve estar entre 0 e 99.");
        if (dto.CostTransport <= 0 || dto.CostTransport > 99)
            return (false, "Transporte deve estar entre 0 e 99.");
        if (dto.Profit < 0 || dto.Profit > 9999)
            return (false, "Lucro % deve estar entre 0 e 9999.");
        if (dto.Stock < 0 || dto.Stock > 32767)
            return (false, "Saldo inválido.");

        try
        {
            var costNet = dto.CostGross * (1 - dto.Discount / 100m);
            var costFinal = costNet * dto.CostTransport;
            var price = costFinal * (1 + dto.Profit / 100m);

            const string sqlProduct = @"
                UPDATE PRODUCT SET
                    COST_GROSS = @CostGross,
                    DISCOUNT = @Discount,
                    COST_NET = @CostNet,
                    COST_TRANSPORT = @CostTransport,
                    COST_FINAL = @CostFinal,
                    STOCK = @Stock,
                    SYS_UPDATE_DATE = GETDATE()
                WHERE PKId = @PKId";

            await _connection.ExecuteAsync(
                new CommandDefinition(sqlProduct, new
                {
                    dto.PKId,
                    dto.CostGross,
                    dto.Discount,
                    CostNet = costNet,
                    dto.CostTransport,
                    CostFinal = costFinal,
                    dto.Stock
                }, cancellationToken: cancellationToken));

            const string sqlMarket = @"
                UPDATE MARKET_PRODUCT SET PROFIT = @Profit, PRICE = @Price
                WHERE PRODUCT_ID = @PKId AND MARKET_ID = @MarketId";
            await _connection.ExecuteAsync(
                new CommandDefinition(sqlMarket, new { dto.PKId, dto.MarketId, dto.Profit, Price = price }, cancellationToken: cancellationToken));

            return (true, "Atualizado");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
