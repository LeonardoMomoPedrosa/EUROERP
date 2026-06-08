using System.Data;
using Dapper;
using EUROERP.Application.Stock;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.Stock;

public class StockInMassService : IStockInMassService
{
    private readonly IDbConnection _connection;

    public StockInMassService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<StockMassProductRowDto>> GetSupplierProductsAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                p.PKId AS ProductId,
                p.NAME AS Name,
                p.EXTERNAL_PKID AS ExternalPkid,
                p.COST_GROSS AS CostGross,
                ISNULL(p.DISCOUNT, 0) AS Discount,
                ISNULL(p.IPI, 0) AS Ipi,
                ISNULL(mp.PROFIT, 0) AS Profit,
                p.CURRENCY_ID AS CurrencyId,
                CONVERT(nvarchar(20), p.CSTB_ID) AS CstbId,
                CASE WHEN u.DECIMAL_IND = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS DecimalInd,
                p.STOCK AS Stock
            FROM PRODUCT p
            JOIN UNITS u ON u.PKId = p.UNIT_ID
            JOIN PRODUCT_SUPPLIER_LINK pl ON p.PKId = pl.PRODUCT_ID
            LEFT JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId AND mp.MARKET_ID = 1
            WHERE pl.SUPPLIER_ID = @SupplierId AND p.ACTIVE = 'Y'
            ORDER BY p.NAME";

        var list = await _connection.QueryAsync<StockMassProductRowDto>(
            new CommandDefinition(sql, new { SupplierId = supplierId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task ApplyMassStockAsync(int supplierId, string externalPkid, IReadOnlyList<StockMassApplyLineDto> lines, string userId, string applicationId, CancellationToken cancellationToken = default)
    {
        var activeLines = lines.Where(l => l.Quantity > 0).ToList();
        if (activeLines.Count == 0)
            return;

        if (_connection is SqlConnection sqlConn && sqlConn.State != ConnectionState.Open)
            sqlConn.Open();

        using var trx = _connection.BeginTransaction();
        try
        {
            var appId = Truncate(applicationId, 8);
            var usrId = Truncate(userId, 20);
            var memoBase = string.IsNullOrWhiteSpace(externalPkid) ? "Entrada em lote" : $"Entrada em lote NF {externalPkid.Trim()}";

            foreach (var line in activeLines)
            {
                var prod = await _connection.QuerySingleAsync<(short Stock, decimal AvgCostFinal, decimal CostTransport)>(
                    new CommandDefinition("SELECT STOCK, AVG_COST_FINAL, COST_TRANSPORT FROM PRODUCT WHERE PKId = @Id",
                        new { Id = line.ProductId }, transaction: trx, cancellationToken: cancellationToken));

                var costNet = line.UnitCostGross * (1 + line.Ipi / 100m) * (1 - line.Discount / 100m);
                var transport = prod.CostTransport > 0 ? prod.CostTransport : 1m;
                var costFinal = costNet * transport;
                var stockAdd = line.DecimalInd ? line.Quantity : Math.Truncate(line.Quantity);
                var newStock = (short)(prod.Stock + stockAdd);
                var newAvg = stockAdd > 0
                    ? (prod.Stock * prod.AvgCostFinal + stockAdd * costFinal) / (prod.Stock + stockAdd)
                    : prod.AvgCostFinal;
                var price = costFinal * (1 + line.Profit / 100m);

                await _connection.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO STOCK_HISTORY (PRODUCT_ID, SYS_CREATION_DATE, USER_ID, CLIENT_ID, PREVIOUS, QUANTITY, MEMO)
                    VALUES (@ProductId, GETDATE(), @UserId, NULL, @Previous, @Qty, @Memo)",
                    new { ProductId = line.ProductId, UserId = usrId, Previous = prod.Stock, Qty = stockAdd, Memo = memoBase },
                    transaction: trx, cancellationToken: cancellationToken));

                await _connection.ExecuteAsync(new CommandDefinition(@"
                    UPDATE PRODUCT SET
                        STOCK = @Stock, STOCK_LAST_IN_DATE = GETDATE(), SYS_UPDATE_DATE = GETDATE(),
                        APPLICATION_ID = @AppId, USER_ID = @UserId,
                        CSTB_ID = CASE WHEN @Cstb IS NOT NULL AND @Cstb <> '' THEN @Cstb ELSE CSTB_ID END,
                        COST_GROSS = @CostGross, IPI = @Ipi, DISCOUNT = @Discount,
                        COST_NET = @CostNet, COST_TRANSPORT = @Transport, COST_FINAL = @CostFinal,
                        AVG_COST_FINAL = @AvgCostFinal
                    WHERE PKId = @ProductId",
                    new
                    {
                        Stock = newStock,
                        AppId = appId,
                        UserId = usrId,
                        Cstb = line.CstbId,
                        CostGross = line.UnitCostGross,
                        line.Ipi,
                        line.Discount,
                        CostNet = costNet,
                        Transport = transport,
                        CostFinal = costFinal,
                        AvgCostFinal = newAvg,
                        line.ProductId
                    }, transaction: trx, cancellationToken: cancellationToken));

                await _connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE MARKET_PRODUCT SET PROFIT = @Profit, PRICE = @Price WHERE PRODUCT_ID = @ProductId AND MARKET_ID = 1",
                    new { line.Profit, Price = price, line.ProductId },
                    transaction: trx, cancellationToken: cancellationToken));
            }

            trx.Commit();
        }
        catch
        {
            trx.Rollback();
            throw;
        }
    }

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return maxLen <= 8 ? "" : "SYS";
        return value.Length <= maxLen ? value : value[..maxLen];
    }
}
