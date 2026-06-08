using System.Data;
using Dapper;
using EUROERP.Application.Stock;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.Stock;

public class PurchaseStockService : IPurchaseStockService
{
    private readonly IDbConnection _connection;

    public PurchaseStockService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<PurchaseStockPendingDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT ps.PURCH_ID AS PurchaseId, ps.SUPPLIER_ID AS SupplierId,
                CONVERT(varchar, p.SYS_CREATION_DATE, 103) AS Date, s.SOCIAL_NAME AS SupplierName
            FROM PURCH_SUPPLIER ps
            JOIN PURCHASE p ON p.PKId = ps.PURCH_ID
            JOIN SUPPLIER s ON s.PKId = ps.SUPPLIER_ID
            WHERE ps.RECEIVED = 0 AND ps.PURCHASED = 1
            ORDER BY ps.PURCH_ID";
        var list = await _connection.QueryAsync<PurchaseStockPendingDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<PurchaseStockHistoryDto>> GetLastStockInsAsync(int rows = 15, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT TOP (@Rows)
                CONVERT(varchar, ps.SYS_CREATION_DATE, 103) AS DateIn,
                CONVERT(varchar, pu.SYS_CREATION_DATE, 103) AS DateOrder,
                DATEDIFF(day, pu.SYS_CREATION_DATE, ps.SYS_CREATION_DATE) AS Days,
                CONVERT(varchar, ps.SYS_CREATION_DATE, 108) AS TimeIn,
                ps.USER_ID AS UserId, ps.SUPPLIER_ID AS SupplierId, ps.PURCH_ID AS PurchaseId,
                s.SOCIAL_NAME AS SupplierName
            FROM PURCH_STOCK ps
            JOIN PURCHASE pu ON pu.PKId = ps.PURCH_ID
            JOIN SUPPLIER s ON s.PKId = ps.SUPPLIER_ID
            ORDER BY ps.SYS_CREATION_DATE DESC";
        var list = await _connection.QueryAsync<PurchaseStockHistoryDto>(
            new CommandDefinition(sql, new { Rows = rows }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<PurchaseStockLineDto>> GetReceiveLinesAsync(int purchaseId, int supplierId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT pd.PRODUCT_ID AS ProductId,
                cast(pg.PRODUCT_CLASS_ID as varchar)+substring('000000'+cast(p.PKId as varchar),len(cast(p.PKId as varchar)),7) as ExternalCode,
                p.EXTERNAL_PKID AS ExternalPkid, p.NAME AS ProductName,
                pd.[ORDER] AS Ordered, pd.RECEIVED AS Received,
                pd.UNIT_PRICE AS UnitPrice, ISNULL(pd.NEW_PRICE, 0) AS NewPrice,
                pd.UNIT_PRICE * puc.CONVERSION * (1-pd.DISCOUNT/100) AS ConvertedPrice
            FROM PURCH_DETAILS pd
            JOIN PURCH_CONVERSION puc ON puc.PURCH_ID = pd.PURCH_ID and puc.CURRENCY_ID = pd.CURRENCY_ID
            JOIN PRODUCT p ON p.PKId = pd.PRODUCT_ID
            JOIN PRODUCT_GROUP pg ON pg.PKId = p.GROUP_ID
            WHERE pd.PURCH_ID = @PurchaseId AND pd.SUPPLIER_ID = @SupplierId AND pd.[ORDER] > 0
            ORDER BY p.NAME";
        var list = await _connection.QueryAsync<PurchaseStockLineDto>(
            new CommandDefinition(sql, new { PurchaseId = purchaseId, SupplierId = supplierId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<PurchaseSupplierReceiptDto?> GetSupplierReceiptAsync(int purchaseId, int supplierId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT RECEIPT_NO AS ReceiptNo, RECEIPT_DATE AS ReceiptDate,
                ISNULL(ICMS_AMOUNT,0) AS IcmsAmount, ISNULL(RECEIPT_AMOUNT,0) AS ReceiptAmount,
                ISNULL(ORDER_AMOUNT,0) AS OrderAmount, ISNULL(BOX_AMOUNT,0) AS BoxAmount,
                ISNULL(GTA_AMOUNT,0) AS GtaAmount, ISNULL(SHIP_AMOUNT,0) AS ShipAmount, MEMO AS Memo
            FROM PURCH_SUPPLIER WHERE PURCH_ID = @PurchaseId AND SUPPLIER_ID = @SupplierId";
        return await _connection.QueryFirstOrDefaultAsync<PurchaseSupplierReceiptDto>(
            new CommandDefinition(sql, new { PurchaseId = purchaseId, SupplierId = supplierId }, cancellationToken: cancellationToken));
    }

    public async Task SaveReceiveDraftAsync(PurchaseStockSaveDto dto, CancellationToken cancellationToken = default)
    {
        await ApplyLinesAsync(dto, onlySave: true, userId: null, applicationId: null, cancellationToken);
        await UpdateReceiptAsync(dto, cancellationToken);
    }

    public async Task FinalizeReceiveAsync(PurchaseStockSaveDto dto, string userId, string applicationId, CancellationToken cancellationToken = default)
    {
        if (_connection is SqlConnection sqlConn && sqlConn.State != ConnectionState.Open)
            sqlConn.Open();

        using var trx = _connection.BeginTransaction();
        try
        {
            await ApplyLinesAsync(dto, onlySave: false, userId, applicationId, cancellationToken, trx);
            await UpdateReceiptAsync(dto, cancellationToken, trx);

            await _connection.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO PURCH_STOCK (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, SUPPLIER_ID, PURCH_ID)
                VALUES (GETDATE(), @UserId, @AppId, @SupplierId, @PurchaseId);
                UPDATE PURCH_SUPPLIER SET RECEIVED = 1 WHERE PURCH_ID = @PurchaseId AND SUPPLIER_ID = @SupplierId",
                new
                {
                    UserId = Truncate(userId, 20),
                    AppId = Truncate(applicationId, 8),
                    dto.SupplierId,
                    dto.PurchaseId
                }, transaction: trx, cancellationToken: cancellationToken));

            trx.Commit();
        }
        catch
        {
            trx.Rollback();
            throw;
        }
    }

    private async Task ApplyLinesAsync(PurchaseStockSaveDto dto, bool onlySave, string? userId, string? applicationId, CancellationToken cancellationToken, IDbTransaction? trx = null)
    {
        foreach (var line in dto.Lines)
        {
            var stock = await _connection.ExecuteScalarAsync<short>(
                new CommandDefinition("SELECT STOCK FROM PRODUCT WHERE PKId = @Id", new { Id = line.ProductId },
                    transaction: trx, cancellationToken: cancellationToken));

            await _connection.ExecuteAsync(new CommandDefinition(@"
                UPDATE PURCH_DETAILS SET RECEIVED = @Received, NEW_PRICE = @NewPrice, PREVIOUS = @Previous
                WHERE PURCH_ID = @PurchaseId AND SUPPLIER_ID = @SupplierId AND PRODUCT_ID = @ProductId",
                new
                {
                    dto.PurchaseId,
                    dto.SupplierId,
                    line.ProductId,
                    line.Received,
                    NewPrice = line.NewPrice,
                    Previous = stock
                }, transaction: trx, cancellationToken: cancellationToken));

            if (!onlySave && line.Received > 0)
            {
                var memo = $"Aplicando estoque de mercadorias na compra {dto.PurchaseId}";
                await _connection.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO STOCK_HISTORY (PRODUCT_ID, SYS_CREATION_DATE, USER_ID, CLIENT_ID, PREVIOUS, QUANTITY, MEMO)
                    VALUES (@ProductId, GETDATE(), @UserId, NULL, @Previous, @Qty, @Memo)",
                    new { ProductId = line.ProductId, UserId = Truncate(userId!, 20), Previous = stock, Qty = line.Received, Memo = memo },
                    transaction: trx, cancellationToken: cancellationToken));

                await _connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE PRODUCT SET STOCK = STOCK + @Qty, STOCK_LAST_IN_DATE = GETDATE(), SYS_UPDATE_DATE = GETDATE(), USER_ID = @UserId, APPLICATION_ID = @AppId WHERE PKId = @ProductId",
                    new { Qty = line.Received, UserId = Truncate(userId!, 20), AppId = Truncate(applicationId!, 8), line.ProductId },
                    transaction: trx, cancellationToken: cancellationToken));
            }
        }
    }

    private async Task UpdateReceiptAsync(PurchaseStockSaveDto dto, CancellationToken cancellationToken, IDbTransaction? trx = null)
    {
        var r = dto.Receipt;
        await _connection.ExecuteAsync(new CommandDefinition(@"
            UPDATE PURCH_SUPPLIER SET
                RECEIPT_NO = @ReceiptNo, RECEIPT_DATE = @ReceiptDate, ICMS_AMOUNT = @IcmsAmount,
                RECEIPT_AMOUNT = @ReceiptAmount, ORDER_AMOUNT = @OrderAmount, BOX_AMOUNT = @BoxAmount,
                GTA_AMOUNT = @GtaAmount, SHIP_AMOUNT = @ShipAmount, MEMO = @Memo
            WHERE PURCH_ID = @PurchaseId AND SUPPLIER_ID = @SupplierId",
            new
            {
                dto.PurchaseId,
                dto.SupplierId,
                r.ReceiptNo,
                r.ReceiptDate,
                r.IcmsAmount,
                r.ReceiptAmount,
                r.OrderAmount,
                r.BoxAmount,
                r.GtaAmount,
                r.ShipAmount,
                Memo = r.Memo ?? ""
            }, transaction: trx, cancellationToken: cancellationToken));
    }

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return maxLen <= 8 ? "" : "SYS";
        return value.Length <= maxLen ? value : value[..maxLen];
    }
}
