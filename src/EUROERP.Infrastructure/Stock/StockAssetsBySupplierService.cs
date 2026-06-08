using System.Data;
using Dapper;
using EUROERP.Application.Stock;

namespace EUROERP.Infrastructure.Stock;

public class StockAssetsBySupplierService : IStockAssetsBySupplierService
{
    private readonly IDbConnection _connection;

    public StockAssetsBySupplierService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<StockAssetsBySupplierRowDto>> GetBySupplierAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                pg.PKId AS GroupId,
                pg.NAME AS GroupName,
                cast(pg.PRODUCT_CLASS_ID as varchar)+substring('000000'+cast(p.PKId as varchar),len(cast(p.PKId as varchar)),7) as LionCode,
                p.NAME AS ProductName,
                isNull(sum(round(round(p.COST_FINAL,2)*p.STOCK,2)*isNull(cv.CONVERSION,1)),0) as CostFinal,
                isNull(sum(round(round(mp.PRICE,2)*p.STOCK,2)*isNull(cv.CONVERSION,1)),0) as Price
            FROM PRODUCT_GROUP pg
            LEFT JOIN PRODUCT p ON p.GROUP_ID = pg.PKId AND p.ACTIVE='Y'
            JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId and mp.MARKET_ID = 1
            JOIN MARKET m ON m.PKId = mp.MARKET_ID
            LEFT JOIN CURRENCY_CONVERSION cv ON p.CURRENCY_ID = cv.TARGET_CURRENCY_ID and cv.SOURCE_CURRENCY_ID = m.CURRENCY_ID
            JOIN PRODUCT_SUPPLIER_LINK psl ON psl.PRODUCT_ID = p.PKId and psl.SUPPLIER_ID = @SupplierId
            GROUP BY pg.NAME, pg.PRODUCT_CLASS_ID, p.CURRENCY_ID, pg.PKId, p.NAME, p.PKId
            ORDER BY pg.PRODUCT_CLASS_ID, pg.NAME";

        var list = await _connection.QueryAsync<StockAssetsBySupplierRowDto>(
            new CommandDefinition(sql, new { SupplierId = supplierId }, cancellationToken: cancellationToken));
        return list.ToList();
    }
}
