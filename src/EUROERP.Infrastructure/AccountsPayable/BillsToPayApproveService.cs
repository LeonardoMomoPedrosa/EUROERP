using System.Data;
using Dapper;
using EUROERP.Application.AccountsPayable;

namespace EUROERP.Infrastructure.AccountsPayable;

public class BillsToPayApproveService : IBillsToPayApproveService
{
    private readonly IDbConnection _connection;

    public BillsToPayApproveService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<BillsToPayReportRowDto>> SearchPendingAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        // Eurobus load_btp_approve: Status "N" → STATUS = 'U' (pending), all dates
        const string sql = @"
SELECT btp.PKId AS PkId, btp.SUPPLIER_ID AS SupplierId,
    CONVERT(VARCHAR, btp.SYS_CREATION_DATE, 103) AS SysCreationDate,
    pm.NAME AS PymMethod, pm.PKId AS PaymentMethodId, btp.TERMS AS Terms,
    btp.USER_ID AS UserId, btp.BILL_TYPE AS BillType, btp.STOCK_IN_ID AS StockInId,
    cu.SYMBOL AS Symbol, btp.CURRENCY_ID AS CurrencyId,
    ISNULL(btp.CONVERSION, 1) AS Conversion,
    su.SOCIAL_NAME AS SocialName,
    btpd.TERM_NO AS TermNo, btpd.STATUS AS Status,
    ROUND(btpd.AMOUNT, 2) AS Amount,
    ROUND(btpd.AMOUNT * ISNULL(btp.CONVERSION, 1), 2) AS ConvertedAmount,
    CONVERT(VARCHAR, btpd.DUE_DATE, 103) AS DueDate,
    CONVERT(VARCHAR, btpd.DUE_DATE, 112) AS DueDateOrder,
    ISNULL(btpd.MEMO, '--') AS Memo,
    ROUND(ISNULL(fp.Paid, 0), 2) AS Paid,
    ROUND(ISNULL(fp.Paid, 0) * ISNULL(btp.CONVERSION, 1), 2) AS ConvertedPaid,
    ISNULL(CONVERT(VARCHAR, btp.ORDER_DATE, 103), '--') AS OrderDate,
    bk.NAME AS Bank, sg.PKId AS Sgid, sg.HIDDEN AS Hidden
FROM FINANCE_BILLS_TO_PAY btp
JOIN FINANCE_BILLS_TO_PAY_DETAIL btpd ON btpd.FINANCE_BILL_ID = btp.PKId
JOIN PAYMENT_METHOD pm ON btp.PAYMENT_METHOD_ID = pm.PKId
JOIN CURRENCY cu ON cu.PKId = btp.CURRENCY_ID
JOIN SUPPLIER su ON su.PKId = btp.SUPPLIER_ID
JOIN SUPPLIER_GROUP sg ON su.SUPPLIER_GROUP_ID = sg.PKId
LEFT JOIN BANK bk ON su.BANK_INFO_BANK_ID = bk.PKId
LEFT JOIN (SELECT FINANCE_BILL_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM FINANCE_PAYMENT GROUP BY FINANCE_BILL_ID, TERM_NO) fp
    ON fp.FINANCE_BILL_ID = btpd.FINANCE_BILL_ID AND fp.TERM_NO = btpd.TERM_NO
WHERE btpd.STATUS = 'U'
  AND (sg.HIDDEN IS NULL OR sg.HIDDEN = '')
ORDER BY btpd.DUE_DATE ASC, su.SOCIAL_NAME, btp.PKId, btpd.TERM_NO";

        var rows = await _connection.QueryAsync<BillsToPayReportRowDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task ApproveAsync(IReadOnlyList<(int BillId, byte TermNo)> items, CancellationToken cancellationToken = default)
    {
        if (items == null || items.Count == 0)
            throw new InvalidOperationException("Nenhuma conta selecionada.");

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using var tx = _connection.BeginTransaction();
        try
        {
            const string sql = @"
UPDATE FINANCE_BILLS_TO_PAY_DETAIL
SET STATUS = 'A'
WHERE FINANCE_BILL_ID = @BillId AND TERM_NO = @TermNo AND STATUS = 'U'";

            foreach (var (billId, termNo) in items)
            {
                await _connection.ExecuteAsync(new CommandDefinition(sql, new { BillId = billId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
