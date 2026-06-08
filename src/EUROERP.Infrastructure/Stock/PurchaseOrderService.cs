using System.Data;
using Dapper;
using EUROERP.Application.Stock;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.Stock;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IDbConnection _connection;

    public PurchaseOrderService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<PurchaseOrderSupplierDto>> GetOrderingSuppliersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT PKId AS Id, SOCIAL_NAME AS SocialName, ISNULL(STOCK_DAYS, 0) AS StockDays
            FROM SUPPLIER WHERE GNRL_ORDERING = 1 ORDER BY SOCIAL_NAME";
        var list = await _connection.QueryAsync<PurchaseOrderSupplierDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<PurchaseSummaryDto>> GetLastPurchasesAsync(int count = 15, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT TOP (@Count)
                CONVERT(VARCHAR, SYS_CREATION_DATE, 103) AS Date,
                USER_ID AS UserId, PKId AS Id, DAYS AS Days
            FROM PURCHASE ORDER BY SYS_CREATION_DATE DESC";
        var list = await _connection.QueryAsync<PurchaseSummaryDto>(
            new CommandDefinition(sql, new { Count = count }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<PurchaseSupplierSummaryDto>> GetPurchaseSuppliersAsync(int purchaseId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT su.PKId AS SupplierId, ps.PURCH_ID AS PurchaseId, su.SOCIAL_NAME AS SupplierName,
                ps.PURCHASED AS Purchased, ps.RECEIVED AS Received,
                SUM(pd.[ORDER]*pd.UNIT_PRICE*(1-pd.DISCOUNT/100)*pc.CONVERSION) AS Amount
            FROM PURCH_SUPPLIER ps
            JOIN SUPPLIER su ON su.PKId = ps.SUPPLIER_ID
            JOIN PURCH_DETAILS pd ON pd.PURCH_ID = ps.PURCH_ID AND pd.SUPPLIER_ID = ps.SUPPLIER_ID
            JOIN PURCH_CONVERSION pc ON pc.PURCH_ID = ps.PURCH_ID AND pc.CURRENCY_ID = pd.CURRENCY_ID
            WHERE ps.PURCH_ID = @PurchaseId
            GROUP BY su.PKId, ps.PURCH_ID, su.SOCIAL_NAME, ps.PURCHASED, ps.RECEIVED";
        var list = await _connection.QueryAsync<PurchaseSupplierSummaryDto>(
            new CommandDefinition(sql, new { PurchaseId = purchaseId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<int> GeneratePurchaseAsync(GeneratePurchaseRequestDto request, string userId, string applicationId, CancellationToken cancellationToken = default)
    {
        if (request.SupplierIds.Count == 0)
            throw new InvalidOperationException("Selecione ao menos um fornecedor.");

        if (_connection is SqlConnection sqlConn && sqlConn.State != ConnectionState.Open)
            sqlConn.Open();

        using var trx = _connection.BeginTransaction();
        try
        {
            var conversionStr = $"1=1;2={request.DollarRate};4={request.DollarVivoRate}";
            var purchaseId = await InsertPurchaseAsync(request.Days, conversionStr, userId, applicationId, trx, cancellationToken);

            foreach (var supplierId in request.SupplierIds)
            {
                var stockDays = await _connection.ExecuteScalarAsync<byte>(
                    new CommandDefinition("SELECT ISNULL(STOCK_DAYS,0) FROM SUPPLIER WHERE PKId = @Id",
                        new { Id = supplierId }, transaction: trx, cancellationToken: cancellationToken));

                await _connection.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO PURCH_SUPPLIER (PURCH_ID, SUPPLIER_ID, STOCK_DAYS, PURCHASED)
                    VALUES (@PurchaseId, @SupplierId, @StockDays, 0)",
                    new { PurchaseId = purchaseId, SupplierId = supplierId, StockDays = stockDays },
                    transaction: trx, cancellationToken: cancellationToken));

                var products = await _connection.QueryAsync<(int Id, int Pack, short Stock, short StockMin, byte CurrencyId, decimal CostGross, decimal Discount)>(
                    new CommandDefinition(@"
                        SELECT p.PKId AS Id, p.PACK AS Pack, p.STOCK AS Stock, p.STOCK_MIN AS StockMin,
                            p.CURRENCY_ID AS CurrencyId, p.COST_GROSS AS CostGross, ISNULL(p.DISCOUNT,0) AS Discount
                        FROM PRODUCT p
                        JOIN PRODUCT_SUPPLIER_LINK pl ON p.PKId = pl.PRODUCT_ID
                        WHERE pl.SUPPLIER_ID = @SupplierId AND p.ACTIVE = 'Y'",
                        new { SupplierId = supplierId }, transaction: trx, cancellationToken: cancellationToken));

                var sales = await _connection.QueryAsync<(int ProductId, decimal Quantity, decimal Ordered)>(
                    new CommandDefinition(@"
                        SELECT od.PRODUCT_ID AS ProductId, SUM(od.QUANTITY) AS Quantity, SUM(od.QTD_ORDERED) AS Ordered
                        FROM ORDER_DETAILS od
                        JOIN [ORDER] o ON o.PKId = od.ORDER_ID
                        JOIN PRODUCT p ON p.PKId = od.PRODUCT_ID
                        JOIN PRODUCT_SUPPLIER_LINK pl ON p.PKId = pl.PRODUCT_ID
                        JOIN CLIENT c ON c.PKId = o.CLIENT_ID
                        WHERE o.STATUS = 'E' AND p.ACTIVE = 'Y' AND pl.SUPPLIER_ID = @SupplierId
                          AND o.SYS_CREATION_DATE > GETDATE() - @Days AND c.COUNT_FOR_ORDERING IS NULL
                        GROUP BY od.PRODUCT_ID",
                        new { SupplierId = supplierId, Days = request.Days }, transaction: trx, cancellationToken: cancellationToken));

                var salesMap = sales.ToDictionary(s => s.ProductId);

                foreach (var p in products)
                {
                    decimal sold = 0, ordered = 0;
                    if (salesMap.TryGetValue(p.Id, out var sale))
                    {
                        sold = sale.Quantity;
                        ordered = sale.Ordered;
                    }
                    var proj = Math.Round((ordered / (decimal)request.Days) * stockDays);
                    var orderQty = GetOrderQty(proj, p.Stock, p.StockMin, p.Pack);

                    await _connection.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO PURCH_DETAILS (PURCH_ID, SUPPLIER_ID, PRODUCT_ID, SOLD, ORDERED, STOCK, PROJ, [ORDER], PACK, UNIT_PRICE, DISCOUNT, CURRENCY_ID, STOCK_MIN)
                        VALUES (@PurchaseId, @SupplierId, @ProductId, @Sold, @Ordered, @Stock, @Proj, @OrderQty, @Pack, @UnitPrice, @Discount, @CurrencyId, @StockMin)",
                        new
                        {
                            PurchaseId = purchaseId,
                            SupplierId = supplierId,
                            ProductId = p.Id,
                            Sold = sold,
                            Ordered = ordered,
                            Stock = p.Stock,
                            Proj = proj,
                            OrderQty = orderQty,
                            Pack = p.Pack,
                            UnitPrice = p.CostGross,
                            Discount = p.Discount,
                            CurrencyId = p.CurrencyId,
                            StockMin = p.StockMin
                        }, transaction: trx, cancellationToken: cancellationToken));
                }
            }

            trx.Commit();
            return purchaseId;
        }
        catch
        {
            trx.Rollback();
            throw;
        }
    }

    public async Task SetPurchasedAsync(int purchaseId, int supplierId, bool purchased, bool cancelPending, CancellationToken cancellationToken = default)
    {
        if (cancelPending)
        {
            const string pendingSql = @"
                SELECT PURCH_ID FROM PURCH_SUPPLIER
                WHERE SUPPLIER_ID = @SupplierId AND PURCHASED = 1 AND RECEIVED = 0";
            var pending = await _connection.QueryAsync<int>(
                new CommandDefinition(pendingSql, new { SupplierId = supplierId }, cancellationToken: cancellationToken));
            foreach (var pid in pending)
            {
                await _connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE PURCH_SUPPLIER SET PURCHASED = 0 WHERE PURCH_ID = @PurchaseId AND SUPPLIER_ID = @SupplierId",
                    new { PurchaseId = pid, SupplierId = supplierId }, cancellationToken: cancellationToken));
            }
        }

        await _connection.ExecuteAsync(new CommandDefinition(
            "UPDATE PURCH_SUPPLIER SET PURCHASED = @Purchased WHERE PURCH_ID = @PurchaseId AND SUPPLIER_ID = @SupplierId",
            new { PurchaseId = purchaseId, SupplierId = supplierId, Purchased = purchased }, cancellationToken: cancellationToken));
    }

    private async Task<int> InsertPurchaseAsync(byte days, string conversionStr, string userId, string applicationId, IDbTransaction trx, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO PURCHASE (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, DAYS)
            VALUES (GETDATE(), @UserId, @AppId, @Days);
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        var purchaseId = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = Truncate(userId, 20), AppId = Truncate(applicationId, 8), Days = days },
                transaction: trx, cancellationToken: cancellationToken));

        foreach (var item in conversionStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split('=');
            if (parts.Length != 2) continue;
            await _connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO PURCH_CONVERSION (PURCH_ID, CURRENCY_ID, CONVERSION) VALUES (@PurchaseId, @CurrencyId, @Conversion)",
                new { PurchaseId = purchaseId, CurrencyId = byte.Parse(parts[0]), Conversion = decimal.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) },
                transaction: trx, cancellationToken: cancellationToken));
        }

        return purchaseId;
    }

    private static int GetOrderQty(decimal proj, int stock, int stockMin, int pack)
    {
        var order = proj >= stockMin ? (int)proj - stock : stockMin - stock;
        order = order < 0 ? 0 : order;
        if (pack <= 0) return order;
        var newOrder = order / pack * pack;
        var tmp = order % pack;
        newOrder += (decimal)tmp / pack >= 0.5m ? pack : 0;
        return newOrder;
    }

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return maxLen <= 8 ? "" : "SYS";
        return value.Length <= maxLen ? value : value[..maxLen];
    }
}
