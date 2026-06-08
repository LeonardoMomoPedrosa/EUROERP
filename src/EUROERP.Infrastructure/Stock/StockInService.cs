using System.Data;
using Dapper;
using EUROERP.Application.Stock;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.Stock;

public class StockInService : IStockInService
{
    private readonly IDbConnection _connection;

    public StockInService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<StockInSupplierDto>> GetSuppliersForStockInAsync(string? name, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT TOP 10
                su.PKId AS Id,
                su.SOCIAL_NAME AS SocialName,
                su.CNPJ AS Cnpj,
                c.NAME AS City
            FROM SUPPLIER su
            JOIN CITY c ON su.ADDRESS_CITY_ID = c.PKId
            JOIN SUPPLIER_GROUP sg ON su.SUPPLIER_GROUP_ID = sg.PKId
            WHERE su.ACTIVE = 'Y'
            AND (sg.HIDDEN IS NULL OR sg.HIDDEN = '')";

        if (!string.IsNullOrWhiteSpace(name))
            sql += " AND su.SOCIAL_NAME LIKE @NameLike";

        sql += " ORDER BY su.SOCIAL_NAME";

        var nameLike = string.IsNullOrWhiteSpace(name) ? null : $"%{name.Trim()}%";
        var list = await _connection.QueryAsync<StockInSupplierDto>(
            new CommandDefinition(sql, new { NameLike = nameLike }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<(int? StockInId, string? Error)> CreateOrGetStockInAsync(StockInHeaderDto header, string userId, string applicationId, CancellationToken cancellationToken = default)
    {
        var extPkid = (header.ExternalPkid ?? "").Trim();
        if (string.IsNullOrEmpty(extPkid))
            return (null, "Número da fatura é obrigatório.");

        const string sqlFind = @"
            SELECT PKId, STATUS
            FROM STOCK_IN
            WHERE SUPPLIER_ID = @SupplierId AND EXTERNAL_PKID = @ExternalPkid";
        var existing = await _connection.QuerySingleOrDefaultAsync<(int PkId, char Status)>(
            new CommandDefinition(sqlFind, new { header.SupplierId, ExternalPkid = extPkid }, cancellationToken: cancellationToken));

        if (existing.PkId != 0 && existing.Status == 'I')
            return (null, "Já existe uma entrada finalizada com este fornecedor e número de fatura.");

        var headerParams = new
        {
            UserId = Truncate(userId, 20),
            ApplicationId = Truncate(applicationId, 8),
            header.ShipCost,
            header.IiCost,
            header.IcmsCost,
            header.PisCost,
            header.CofinsCost,
            header.CreditAmount,
            NfeInd = header.NfeInd,
            header.NfeAmount
        };

        if (existing.PkId != 0)
        {
            const string sqlUpdate = @"
                UPDATE STOCK_IN SET
                    SYS_UPDATE_DATE = GETDATE(),
                    USER_ID = @UserId,
                    APPLICATION_ID = @ApplicationId,
                    DRIVER_COST = 0,
                    GTA_COST = 0,
                    BOX_COST = 0,
                    SHIP_COST = @ShipCost,
                    II = @IiCost,
                    ICMS = @IcmsCost,
                    PIS = @PisCost,
                    COFINS = @CofinsCost,
                    CREDIT_AMOUNT = @CreditAmount,
                    NFE_IND = @NfeInd,
                    NFE_AMOUNT = @NfeAmount,
                    COST_TRANSPORT = 1,
                    COST_TRANSPORT_SUP = 1
                WHERE PKId = @Id";
            await _connection.ExecuteAsync(
                new CommandDefinition(sqlUpdate, new { headerParams.UserId, headerParams.ApplicationId, headerParams.ShipCost, headerParams.IiCost, headerParams.IcmsCost, headerParams.PisCost, headerParams.CofinsCost, headerParams.CreditAmount, headerParams.NfeInd, headerParams.NfeAmount, Id = existing.PkId }, cancellationToken: cancellationToken));
            return (existing.PkId, null);
        }

        const string sqlInsert = @"
            INSERT INTO STOCK_IN (
                SUPPLIER_ID, SYS_CREATION_DATE, USER_ID, APPLICATION_ID, EXTERNAL_PKID,
                DRIVER_COST, GTA_COST, BOX_COST, SHIP_COST, PRODUCTS_COST, PRODUCT_TYPE,
                CONVERSION_TAX, CURRENCY_ID, CREDIT_AMOUNT, STATUS, NFE_IND, NFE_AMOUNT,
                COST_TRANSPORT, COST_TRANSPORT_SUP, II, ICMS, PIS, COFINS)
            VALUES (
                @SupplierId, GETDATE(), @UserId, @ApplicationId, @ExternalPkid,
                0, 0, 0, @ShipCost, 0, 1,
                1, @CurrencyId, @CreditAmount, 'N', @NfeInd, @NfeAmount,
                1, 1, @IiCost, @IcmsCost, @PisCost, @CofinsCost);
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        var newId = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlInsert, new
            {
                header.SupplierId,
                headerParams.UserId,
                headerParams.ApplicationId,
                ExternalPkid = extPkid,
                headerParams.ShipCost,
                headerParams.IiCost,
                headerParams.IcmsCost,
                headerParams.PisCost,
                headerParams.CofinsCost,
                headerParams.CreditAmount,
                headerParams.NfeInd,
                headerParams.NfeAmount,
                header.CurrencyId
            }, cancellationToken: cancellationToken));
        return (newId, null);
    }

    public async Task<IReadOnlyList<StockInSummaryRowDto>> GetStockInSummaryByDaysAsync(int days, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.PKId AS Id, s.SYS_CREATION_DATE AS SysCreationDate, su.SOCIAL_NAME AS SupplierName,
                s.EXTERNAL_PKID AS ExternalPkid, (s.PRODUCTS_COST - ISNULL(s.CREDIT_AMOUNT, 0)) AS Cost, s.STATUS AS Status,
                c.SYMBOL AS CurrencySymbol
            FROM STOCK_IN s
            JOIN SUPPLIER su ON s.SUPPLIER_ID = su.PKId
            JOIN CURRENCY c ON s.CURRENCY_ID = c.PKId
            WHERE s.SYS_CREATION_DATE >= DATEADD(day, -@Days, GETDATE())
            ORDER BY s.SYS_CREATION_DATE DESC";
        var list = await _connection.QueryAsync<StockInSummaryRowDto>(
            new CommandDefinition(sql, new { Days = days }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<StockInHeaderDto?> GetStockInHeaderAsync(int stockInId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.PKId AS Id, s.SUPPLIER_ID AS SupplierId, su.SOCIAL_NAME AS SupplierName,
                s.USER_ID AS UserId,
                s.EXTERNAL_PKID AS ExternalPkid,
                s.SHIP_COST AS ShipCost, s.II AS IiCost, s.ICMS AS IcmsCost,
                s.PIS AS PisCost, s.COFINS AS CofinsCost, s.CREDIT_AMOUNT AS CreditAmount,
                s.NFE_IND AS NfeInd, s.NFE_AMOUNT AS NfeAmount,
                s.PRODUCTS_COST AS ProductsCost, s.CURRENCY_ID AS CurrencyId, s.STATUS AS Status
            FROM STOCK_IN s
            JOIN SUPPLIER su ON s.SUPPLIER_ID = su.PKId
            WHERE s.PKId = @StockInId";
        return await _connection.QuerySingleOrDefaultAsync<StockInHeaderDto>(
            new CommandDefinition(sql, new { StockInId = stockInId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<StockInDetailItemDto>> GetStockInDetailsAsync(int stockInId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                t1.STOCK_IN_ID AS StockInId,
                t1.PRODUCT_ID AS ProductId,
                t1.QUANTITY AS Quantity,
                t1.NAME AS ProductName,
                p.EXTERNAL_PKID AS ExternalId,
                t1.UNIT_COST_GROSS AS UnitCostGross,
                t1.IPI AS Ipi,
                t1.DISCOUNT AS Discount,
                t1.UNIT_COST_NET AS UnitCostNet,
                t1.S_COST_TRANSPORT AS CostTransport,
                t1.S_UNIT_COST_FINAL AS UnitCostFinal,
                t1.TOTAL_COST AS TotalCost,
                t1.S_PROFIT AS Profit,
                t1.S_UNIT_MARKET_PRICE AS UnitMarketPrice,
                p.CSTB_ID AS CstbId,
                t1.CURRENCY_ID AS CurrencyId,
                t1.P_STOCK AS PreviousStock
            FROM (
                SELECT
                    t.TOTAL_TO_RATE * t.WEIGHT AS RATED,
                    t.UNIT_COST_NET + CASE WHEN t.QUANTITY > 0 THEN t.TOTAL_TO_RATE * t.WEIGHT / t.QUANTITY ELSE 0 END AS S_UNIT_COST_FINAL,
                    CASE WHEN t.UNIT_COST_NET > 0
                        THEN (t.UNIT_COST_NET + CASE WHEN t.QUANTITY > 0 THEN t.TOTAL_TO_RATE * t.WEIGHT / t.QUANTITY ELSE 0 END) / t.UNIT_COST_NET
                        ELSE 1 END AS S_COST_TRANSPORT,
                    (t.UNIT_COST_NET + CASE WHEN t.QUANTITY > 0 THEN t.TOTAL_TO_RATE * t.WEIGHT / t.QUANTITY ELSE 0 END) * (1 + t.S_PROFIT / 100) AS S_UNIT_MARKET_PRICE,
                    t.*
                FROM (
                    SELECT
                        p.NAME,
                        sid.STOCK_IN_ID,
                        sid.PRODUCT_ID,
                        p.STOCK AS P_STOCK,
                        sid.PROFIT AS S_PROFIT,
                        sid.QUANTITY,
                        CASE WHEN (SELECT SUM(TOTAL_COST) FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId) > 0
                            THEN sid.UNIT_COST_NET * sid.QUANTITY / (SELECT SUM(TOTAL_COST) FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId)
                            ELSE 0 END AS WEIGHT,
                        (ISNULL(si.SHIP_COST, 0) + ISNULL(si.II, 0) + ISNULL(si.ICMS, 0) + ISNULL(si.PIS, 0) + ISNULL(si.COFINS, 0) - ISNULL(si.CREDIT_AMOUNT, 0)) AS TOTAL_TO_RATE,
                        sid.UNIT_COST_GROSS,
                        sid.DISCOUNT,
                        sid.IPI,
                        sid.UNIT_COST_NET,
                        sid.TOTAL_COST,
                        sid.CURRENCY_ID
                    FROM STOCK_IN_DETAIL sid
                    JOIN PRODUCT p ON p.PKId = sid.PRODUCT_ID
                    JOIN STOCK_IN si ON sid.STOCK_IN_ID = si.PKId
                    WHERE sid.STOCK_IN_ID = @StockInId
                ) t
            ) t1
            JOIN PRODUCT p ON p.PKId = t1.PRODUCT_ID
            ORDER BY t1.NAME";

        var list = await _connection.QueryAsync<StockInDetailItemDto>(
            new CommandDefinition(sql, new { StockInId = stockInId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task AddStockInDetailAsync(int stockInId, StockInAddDetailDto detail, CancellationToken cancellationToken = default)
    {
        var unitCostNet = GetCostNet(detail.UnitCostGross, detail.Ipi, detail.Discount);
        var transport = detail.CostTransport > 0 ? detail.CostTransport : 1m;
        var unitCostFinal = GetCostFinal(unitCostNet, transport);
        var unitMarketPrice = detail.UnitMarketPrice > 0
            ? detail.UnitMarketPrice
            : GetPrice(unitCostFinal, detail.Profit);
        var totalCost = unitCostNet * detail.Quantity;

        const string sqlPrevious = "SELECT STOCK FROM PRODUCT WHERE PKId = @ProductId";
        var previous = await _connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sqlPrevious, new { detail.ProductId }, cancellationToken: cancellationToken));

        const string sqlExists = "SELECT 1 FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId AND PRODUCT_ID = @ProductId";
        var exists = await _connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(sqlExists, new { StockInId = stockInId, detail.ProductId }, cancellationToken: cancellationToken));

        if (exists == 1)
        {
            const string sqlCurrent = @"
                SELECT QUANTITY, UNIT_COST_GROSS, IPI, DISCOUNT, UNIT_COST_NET, COST_TRANSPORT, UNIT_COST_FINAL,
                       TOTAL_COST, PROFIT, UNIT_MARKET_PRICE, PREVIOUS
                FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId AND PRODUCT_ID = @ProductId";
            var row = await _connection.QuerySingleAsync<(decimal Qty, decimal Gross, decimal Ipi, decimal Disc, decimal Net, decimal Transport, decimal Final, decimal Total, decimal Profit, decimal MarketPrice, decimal Prev)>(
                new CommandDefinition(sqlCurrent, new { StockInId = stockInId, detail.ProductId }, cancellationToken: cancellationToken));

            var newQty = row.Qty + detail.Quantity;
            var newTotal = row.Net * newQty;
            const string sqlDelete = "DELETE FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId AND PRODUCT_ID = @ProductId";
            await _connection.ExecuteAsync(
                new CommandDefinition(sqlDelete, new { StockInId = stockInId, detail.ProductId }, cancellationToken: cancellationToken));

            await InsertDetailRowAsync(stockInId, detail.ProductId, newQty, row.Gross, row.Ipi, row.Disc, row.Net, row.Transport, row.Final, newTotal, row.Profit, row.MarketPrice, detail.CurrencyId, row.Prev, cancellationToken);
        }
        else
        {
            await InsertDetailRowAsync(stockInId, detail.ProductId, detail.Quantity, detail.UnitCostGross, detail.Ipi, detail.Discount,
                unitCostNet, transport, unitCostFinal, totalCost, detail.Profit, unitMarketPrice, detail.CurrencyId, previous, cancellationToken);
        }

        await UpdateStockInProductsCostAsync(stockInId, cancellationToken);
    }

    public async Task RemoveStockInDetailAsync(int stockInId, int productId, CancellationToken cancellationToken = default)
    {
        await _connection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId AND PRODUCT_ID = @ProductId",
                new { StockInId = stockInId, ProductId = productId },
                cancellationToken: cancellationToken));
        await UpdateStockInProductsCostAsync(stockInId, cancellationToken);
    }

    public async Task RemoveAllDetailsAsync(int stockInId, CancellationToken cancellationToken = default)
    {
        await _connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId", new { StockInId = stockInId }, cancellationToken: cancellationToken));
        await UpdateStockInProductsCostAsync(stockInId, cancellationToken);
    }

    public async Task<IReadOnlyList<StockInProductSearchDto>> GetProductSuggestionsAsync(string term, int? supplierId, int limit, CancellationToken cancellationToken = default)
    {
        var t = (term ?? "").Trim();
        if (t.Length < 2)
            return new List<StockInProductSearchDto>();

        var nameLike = $"%{t}%";
        var sql = @"
            SELECT TOP (@Limit)
                p.PKId AS Id,
                p.NAME AS Name,
                p.EXTERNAL_PKID AS ExternalPkid,
                p.COST_GROSS AS CostGross,
                p.COST_NET AS CostNet,
                ISNULL(p.IPI, 0) AS Ipi,
                ISNULL(p.DISCOUNT, 0) AS Discount,
                p.COST_TRANSPORT AS CostTransport,
                p.COST_FINAL AS CostFinal,
                ISNULL(mp.PRICE, p.COST_FINAL) AS Price,
                p.STOCK AS Stock,
                p.CURRENCY_ID AS CurrencyId,
                p.CSTB_ID AS CstbId
            FROM PRODUCT p
            LEFT JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId AND mp.MARKET_ID = 1
            WHERE p.ACTIVE = 'Y'
            AND (p.NAME LIKE @NameLike OR p.EXTERNAL_PKID LIKE @NameLike)
            ORDER BY p.NAME";

        var list = await _connection.QueryAsync<StockInProductSearchDto>(
            new CommandDefinition(sql, new { NameLike = nameLike, Limit = limit }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<StockInProductSearchDto?> GetProductForStockInAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT p.PKId AS Id, p.NAME AS Name, p.EXTERNAL_PKID AS ExternalPkid,
                p.COST_GROSS AS CostGross, p.COST_NET AS CostNet, ISNULL(p.IPI, 0) AS Ipi,
                ISNULL(p.DISCOUNT, 0) AS Discount,
                p.COST_TRANSPORT AS CostTransport, p.COST_FINAL AS CostFinal,
                ISNULL(mp.PRICE, p.COST_FINAL) AS Price, p.STOCK AS Stock, p.CURRENCY_ID AS CurrencyId,
                p.CSTB_ID AS CstbId
            FROM PRODUCT p
            LEFT JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId AND mp.MARKET_ID = 1
            WHERE p.PKId = @ProductId";
        return await _connection.QuerySingleOrDefaultAsync<StockInProductSearchDto>(
            new CommandDefinition(sql, new { ProductId = productId }, cancellationToken: cancellationToken));
    }

    public async Task<decimal> CalculateTotalAsync(int stockInId, CancellationToken cancellationToken = default)
    {
        var total = await _connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition("SELECT ISNULL(SUM(TOTAL_COST), 0) FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId", new { StockInId = stockInId }, cancellationToken: cancellationToken));
        return total ?? 0;
    }

    public async Task FinalizeStockInAsync(int stockInId, string userId, string applicationId, IReadOnlyDictionary<int, string>? cstbByProductId = null, CancellationToken cancellationToken = default)
    {
        var details = (await GetStockInDetailsAsync(stockInId, cancellationToken)).ToList();
        if (details.Count == 0)
            return;

        if (_connection is SqlConnection sqlConn && sqlConn.State != ConnectionState.Open)
            sqlConn.Open();

        using var trx = _connection.BeginTransaction();
        try
        {
            var appId = Truncate(applicationId, 8);
            var usrId = Truncate(userId, 20);
            var memo = $"Entrada manual de estoque #{stockInId}";

            foreach (var d in details)
            {
                const string sqlProduct = "SELECT STOCK, AVG_COST_FINAL FROM PRODUCT WHERE PKId = @ProductId";
                var prod = await _connection.QuerySingleAsync<(decimal Stock, decimal AvgCostFinal)>(
                    new CommandDefinition(sqlProduct, new { d.ProductId }, cancellationToken: cancellationToken, transaction: trx));

                var costNet = GetCostNet(d.UnitCostGross, d.Ipi, d.Discount);
                var costTransport = d.CostTransport > 0 ? d.CostTransport : 1m;
                var costFinal = d.UnitCostFinal > 0 ? d.UnitCostFinal : GetCostFinal(costNet, costTransport);
                var price = d.UnitMarketPrice > 0 ? d.UnitMarketPrice : GetPrice(costFinal, d.Profit);
                var cstbId = cstbByProductId != null && cstbByProductId.TryGetValue(d.ProductId, out var selectedCstb) && !string.IsNullOrEmpty(selectedCstb)
                    ? selectedCstb
                    : (string.IsNullOrEmpty(d.CstbId) ? "" : d.CstbId);

                await _connection.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO STOCK_HISTORY (PRODUCT_ID, SYS_CREATION_DATE, USER_ID, CLIENT_ID, PREVIOUS, QUANTITY, MEMO)
                    VALUES (@ProductId, GETDATE(), @UserId, NULL, @Previous, @Qty, @Memo)",
                    new { d.ProductId, UserId = usrId, Previous = prod.Stock, Qty = d.Quantity, Memo = memo },
                    cancellationToken: cancellationToken, transaction: trx));

                var newStock = prod.Stock + d.Quantity;
                var newAvgCostFinal = d.Quantity > 0
                    ? (prod.Stock * prod.AvgCostFinal + d.Quantity * costFinal) / (prod.Stock + d.Quantity)
                    : prod.AvgCostFinal;

                const string sqlUpdateProduct = @"
                    UPDATE PRODUCT SET
                        STOCK = @Stock,
                        STOCK_LAST_IN_DATE = GETDATE(),
                        SYS_UPDATE_DATE = GETDATE(),
                        APPLICATION_ID = @ApplicationId,
                        USER_ID = @UserId,
                        CSTB_ID = @CstbId,
                        COST_GROSS = @CostGross,
                        IPI = @Ipi,
                        DISCOUNT = @Discount,
                        COST_NET = @CostNet,
                        COST_TRANSPORT = @CostTransport,
                        COST_FINAL = @CostFinal,
                        AVG_COST_FINAL = @AvgCostFinal
                    WHERE PKId = @ProductId";
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlUpdateProduct, new
                    {
                        d.ProductId,
                        Stock = newStock,
                        ApplicationId = appId,
                        UserId = usrId,
                        CstbId = cstbId,
                        CostGross = d.UnitCostGross,
                        d.Ipi,
                        d.Discount,
                        CostNet = costNet,
                        CostTransport = costTransport,
                        CostFinal = costFinal,
                        AvgCostFinal = newAvgCostFinal
                    }, cancellationToken: cancellationToken, transaction: trx));

                const string sqlMarkets = "SELECT MARKET_ID FROM MARKET_PRODUCT WHERE PRODUCT_ID = @ProductId";
                var marketIds = await _connection.QueryAsync<byte>(
                    new CommandDefinition(sqlMarkets, new { d.ProductId }, cancellationToken: cancellationToken, transaction: trx));
                foreach (var mktId in marketIds)
                {
                    const string sqlUpdateMp = "UPDATE MARKET_PRODUCT SET PROFIT = @Profit, PRICE = @Price WHERE PRODUCT_ID = @ProductId AND MARKET_ID = @MarketId";
                    await _connection.ExecuteAsync(
                        new CommandDefinition(sqlUpdateMp, new { d.ProductId, MarketId = mktId, d.Profit, Price = price }, cancellationToken: cancellationToken, transaction: trx));
                }
            }

            const string sqlStatus = "UPDATE STOCK_IN SET STATUS = 'I', SYS_UPDATE_DATE = GETDATE(), USER_ID = @UserId, APPLICATION_ID = @ApplicationId WHERE PKId = @StockInId";
            await _connection.ExecuteAsync(
                new CommandDefinition(sqlStatus, new { StockInId = stockInId, UserId = usrId, ApplicationId = appId }, cancellationToken: cancellationToken, transaction: trx));

            trx.Commit();
        }
        catch
        {
            trx.Rollback();
            throw;
        }
    }

    private async Task InsertDetailRowAsync(
        int stockInId, int productId, decimal quantity, decimal unitCostGross, decimal ipi, decimal discount,
        decimal unitCostNet, decimal costTransport, decimal unitCostFinal, decimal totalCost, decimal profit,
        decimal unitMarketPrice, byte currencyId, decimal previous, CancellationToken cancellationToken)
    {
        const string sqlInsert = @"
            INSERT INTO STOCK_IN_DETAIL (
                STOCK_IN_ID, PRODUCT_ID, QUANTITY, UNIT_COST_GROSS, IPI, DISCOUNT, UNIT_COST_NET,
                COST_TRANSPORT, UNIT_COST_FINAL, TOTAL_COST, CURRENCY_ID, PREVIOUS, PROFIT, UNIT_MARKET_PRICE)
            VALUES (
                @StockInId, @ProductId, @Quantity, @UnitCostGross, @Ipi, @Discount, @UnitCostNet,
                @CostTransport, @UnitCostFinal, @TotalCost, @CurrencyId, @Previous, @Profit, @UnitMarketPrice)";
        await _connection.ExecuteAsync(
            new CommandDefinition(sqlInsert, new
            {
                StockInId = stockInId,
                ProductId = productId,
                Quantity = quantity,
                UnitCostGross = unitCostGross,
                Ipi = ipi,
                Discount = discount,
                UnitCostNet = unitCostNet,
                CostTransport = costTransport,
                UnitCostFinal = unitCostFinal,
                TotalCost = totalCost,
                CurrencyId = currencyId,
                Previous = previous,
                Profit = profit,
                UnitMarketPrice = unitMarketPrice
            }, cancellationToken: cancellationToken));
    }

    private async Task UpdateStockInProductsCostAsync(int stockInId, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE STOCK_IN SET PRODUCTS_COST = (SELECT ISNULL(SUM(TOTAL_COST), 0) FROM STOCK_IN_DETAIL WHERE STOCK_IN_ID = @StockInId) WHERE PKId = @StockInId";
        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { StockInId = stockInId }, cancellationToken: cancellationToken));
    }

    private static decimal GetCostNet(decimal costGross, decimal ipi, decimal discount) =>
        costGross * (1 + ipi / 100m) * (1 - discount / 100m);

    private static decimal GetCostFinal(decimal costNet, decimal costTransport) =>
        costNet * costTransport;

    private static decimal GetPrice(decimal costFinal, decimal profit) =>
        costFinal * (1 + profit / 100m);

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return maxLen <= 8 ? "" : "SYS";
        return value.Length <= maxLen ? value : value[..maxLen];
    }
}
