using System.Data;
using Dapper;
using EUROERP.Application.AccountsPayable;

namespace EUROERP.Infrastructure.AccountsPayable;

public class UpdateBillsToPayService : IUpdateBillsToPayService
{
    private readonly IDbConnection _connection;

    public UpdateBillsToPayService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<BillToPayDetailDto?> GetDetailAsync(int billId, byte termNo, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"
SELECT btp.PKId AS BillId, btpd.TERM_NO AS TermNo, btp.TERMS AS Terms,
    btpd.DUE_DATE AS DueDate, btp.ORDER_DATE AS OrderDate,
    ROUND(btpd.AMOUNT, 2) AS Amount,
    ROUND(ISNULL(fp.Paid, 0), 2) AS Paid,
    ISNULL(btpd.MEMO, '') AS Memo, btp.PAYMENT_METHOD_ID AS PaymentMethodId,
    su.SOCIAL_NAME AS SupplierName, cu.SYMBOL AS Symbol, btpd.STATUS AS Status
FROM FINANCE_BILLS_TO_PAY btp
JOIN FINANCE_BILLS_TO_PAY_DETAIL btpd ON btpd.FINANCE_BILL_ID = btp.PKId
JOIN SUPPLIER su ON su.PKId = btp.SUPPLIER_ID
JOIN CURRENCY cu ON cu.PKId = btp.CURRENCY_ID
LEFT JOIN (SELECT FINANCE_BILL_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM FINANCE_PAYMENT GROUP BY FINANCE_BILL_ID, TERM_NO) fp
    ON fp.FINANCE_BILL_ID = btpd.FINANCE_BILL_ID AND fp.TERM_NO = btpd.TERM_NO
WHERE btp.PKId = @BillId AND btpd.TERM_NO = @TermNo";
        return await _connection.QueryFirstOrDefaultAsync<BillToPayDetailDto>(
            new CommandDefinition(sql, new { BillId = billId, TermNo = termNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpdateDueDateAsync(int billId, byte termNo, DateTime dueDate, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        using var tx = _connection.BeginTransaction();
        try
        {
            await InsertBtpHistoryAsync(billId, termNo, applicationId, userId, tx, cancellationToken).ConfigureAwait(false);
            const string sql = @"UPDATE FINANCE_BILLS_TO_PAY_DETAIL SET DUE_DATE = @DueDate WHERE FINANCE_BILL_ID = @BillId AND TERM_NO = @TermNo";
            await _connection.ExecuteAsync(new CommandDefinition(sql, new { DueDate = dueDate, BillId = billId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateOrderDateAsync(int billId, DateTime orderDate, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"UPDATE FINANCE_BILLS_TO_PAY SET ORDER_DATE = @OrderDate WHERE PKId = @BillId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { OrderDate = orderDate, BillId = billId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpdateAmountAsync(int billId, byte termNo, decimal amount, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        using var tx = _connection.BeginTransaction();
        try
        {
            await InsertBtpHistoryAsync(billId, termNo, applicationId, userId, tx, cancellationToken).ConfigureAwait(false);

            var detail = await GetDetailInTxAsync(billId, termNo, tx, cancellationToken).ConfigureAwait(false);
            if (detail == null)
            {
                tx.Commit();
                return;
            }

            var newStatus = (amount - detail.Paid) <= 0 ? "P" : detail.Status;
            const string sql = @"UPDATE FINANCE_BILLS_TO_PAY_DETAIL SET AMOUNT = @Amount, STATUS = @Status WHERE FINANCE_BILL_ID = @BillId AND TERM_NO = @TermNo";
            await _connection.ExecuteAsync(new CommandDefinition(sql, new { Amount = amount, Status = newStatus, BillId = billId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateMemoAsync(int billId, byte termNo, string memo, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        if (memo.Length > 200) memo = memo[..200];

        using var tx = _connection.BeginTransaction();
        try
        {
            await InsertBtpHistoryAsync(billId, termNo, applicationId, userId, tx, cancellationToken).ConfigureAwait(false);
            const string sql = @"UPDATE FINANCE_BILLS_TO_PAY_DETAIL SET MEMO = @Memo WHERE FINANCE_BILL_ID = @BillId AND TERM_NO = @TermNo";
            await _connection.ExecuteAsync(new CommandDefinition(sql, new { Memo = memo ?? "", BillId = billId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdatePaymentMethodAsync(int billId, byte paymentMethodId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"UPDATE FINANCE_BILLS_TO_PAY SET PAYMENT_METHOD_ID = @PaymentMethodId WHERE PKId = @BillId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { PaymentMethodId = paymentMethodId, BillId = billId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Eurobus FinancialBillsController.insertBtpHistory — snapshot before change.</summary>
    private async Task InsertBtpHistoryAsync(int billId, byte termNo, string applicationId, string userId, IDbTransaction tx, CancellationToken cancellationToken)
    {
        applicationId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        userId = userId.Length > 20 ? userId[..20] : userId;

        const string sql = @"
INSERT INTO FINANCE_BTP_CHG_HST
    (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, FINANCE_BILL_ID, TERM_NO, AMOUNT, STATUS, MEMO, DUE_DATE)
SELECT GETDATE(), @UserId, @ApplicationId, btpd.FINANCE_BILL_ID, btpd.TERM_NO, btpd.AMOUNT, btpd.STATUS, btpd.MEMO, btpd.DUE_DATE
FROM FINANCE_BILLS_TO_PAY_DETAIL btpd
WHERE btpd.FINANCE_BILL_ID = @BillId AND btpd.TERM_NO = @TermNo";

        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            ApplicationId = applicationId,
            BillId = billId,
            TermNo = termNo
        }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<BillToPayDetailDto?> GetDetailInTxAsync(int billId, byte termNo, IDbTransaction tx, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT btp.PKId AS BillId, btpd.TERM_NO AS TermNo, btp.TERMS AS Terms,
    btpd.DUE_DATE AS DueDate, btp.ORDER_DATE AS OrderDate,
    ROUND(btpd.AMOUNT, 2) AS Amount,
    ROUND(ISNULL(fp.Paid, 0), 2) AS Paid,
    ISNULL(btpd.MEMO, '') AS Memo, btp.PAYMENT_METHOD_ID AS PaymentMethodId,
    su.SOCIAL_NAME AS SupplierName, cu.SYMBOL AS Symbol, btpd.STATUS AS Status
FROM FINANCE_BILLS_TO_PAY btp
JOIN FINANCE_BILLS_TO_PAY_DETAIL btpd ON btpd.FINANCE_BILL_ID = btp.PKId
JOIN SUPPLIER su ON su.PKId = btp.SUPPLIER_ID
JOIN CURRENCY cu ON cu.PKId = btp.CURRENCY_ID
LEFT JOIN (SELECT FINANCE_BILL_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM FINANCE_PAYMENT GROUP BY FINANCE_BILL_ID, TERM_NO) fp
    ON fp.FINANCE_BILL_ID = btpd.FINANCE_BILL_ID AND fp.TERM_NO = btpd.TERM_NO
WHERE btp.PKId = @BillId AND btpd.TERM_NO = @TermNo";
        return await _connection.QueryFirstOrDefaultAsync<BillToPayDetailDto>(
            new CommandDefinition(sql, new { BillId = billId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private void EnsureOpen()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }
}
