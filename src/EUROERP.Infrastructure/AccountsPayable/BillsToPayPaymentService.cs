using System.Data;
using Dapper;
using EUROERP.Application.AccountsPayable;

namespace EUROERP.Infrastructure.AccountsPayable;

public class BillsToPayPaymentService : IBillsToPayPaymentService
{
    private readonly IDbConnection _connection;

    public BillsToPayPaymentService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<PaymentRowDto>> GetPaymentsAsync(int billId, byte termNo, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sql = @"
SELECT PKId AS PkId, CONVERT(VARCHAR, PAYMENT_DATE, 103) AS PaymentDate,
    CONVERT(VARCHAR, SYS_CREATION_DATE, 113) AS SysCreationDate,
    ROUND(AMOUNT, 2) AS Amount, ISNULL(MEMO, '') AS Memo, USER_ID AS UserId
FROM FINANCE_PAYMENT
WHERE FINANCE_BILL_ID = @BillId AND TERM_NO = @TermNo
ORDER BY PAYMENT_DATE";
        var list = await _connection.QueryAsync<PaymentRowDto>(
            new CommandDefinition(sql, new { BillId = billId, TermNo = termNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task RegisterPaymentAsync(int billId, byte termNo, decimal amount, DateTime paymentDate, string? memo, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        applicationId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        userId = userId.Length > 20 ? userId[..20] : userId;
        memo = memo?.Length > 200 ? memo[..200] : (memo ?? "");

        const string leftSql = @"
SELECT ROUND(btpd.AMOUNT, 2) - ROUND(ISNULL(fp.Paid, 0), 2) AS LeftAmount
FROM FINANCE_BILLS_TO_PAY_DETAIL btpd
LEFT JOIN (SELECT FINANCE_BILL_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM FINANCE_PAYMENT GROUP BY FINANCE_BILL_ID, TERM_NO) fp
    ON fp.FINANCE_BILL_ID = btpd.FINANCE_BILL_ID AND fp.TERM_NO = btpd.TERM_NO
WHERE btpd.FINANCE_BILL_ID = @BillId AND btpd.TERM_NO = @TermNo";
        var leftAmount = await _connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(leftSql, new { BillId = billId, TermNo = termNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        using var tx = _connection.BeginTransaction();
        try
        {
            if (amount >= leftAmount && leftAmount > 0)
            {
                const string closeSql = @"UPDATE FINANCE_BILLS_TO_PAY_DETAIL SET STATUS = 'P' WHERE FINANCE_BILL_ID = @BillId AND TERM_NO = @TermNo";
                await _connection.ExecuteAsync(new CommandDefinition(closeSql, new { BillId = billId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            if (amount > leftAmount && leftAmount > 0)
            {
                const string updateAmountSql = @"UPDATE FINANCE_BILLS_TO_PAY_DETAIL SET AMOUNT = @Amount, STATUS = 'P' WHERE FINANCE_BILL_ID = @BillId AND TERM_NO = @TermNo";
                var currentDetail = await _connection.QueryFirstOrDefaultAsync<(decimal Amount, decimal Paid)>(new CommandDefinition(
                    @"SELECT ROUND(btpd.AMOUNT, 2), ROUND(ISNULL(fp.Paid, 0), 2) FROM FINANCE_BILLS_TO_PAY_DETAIL btpd
LEFT JOIN (SELECT FINANCE_BILL_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM FINANCE_PAYMENT GROUP BY FINANCE_BILL_ID, TERM_NO) fp ON fp.FINANCE_BILL_ID = btpd.FINANCE_BILL_ID AND fp.TERM_NO = btpd.TERM_NO
WHERE btpd.FINANCE_BILL_ID = @BillId AND btpd.TERM_NO = @TermNo", new { BillId = billId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
                var newAmount = currentDetail.Paid + amount;
                await _connection.ExecuteAsync(new CommandDefinition(updateAmountSql, new { Amount = newAmount, BillId = billId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            const string insertSql = @"
INSERT INTO FINANCE_PAYMENT (FINANCE_BILL_ID, TERM_NO, SYS_CREATION_DATE, USER_ID, APPLICATION_ID, AMOUNT, PAYMENT_DATE, MEMO)
VALUES (@BillId, @TermNo, GETDATE(), @UserId, @ApplicationId, @Amount, @PaymentDate, @Memo)";
            await _connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                BillId = billId,
                TermNo = termNo,
                UserId = userId,
                ApplicationId = applicationId,
                Amount = amount,
                PaymentDate = paymentDate,
                Memo = memo
            }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task DeletePaymentAsync(int paymentPkId, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string getBillSql = @"SELECT FINANCE_BILL_ID AS BillId, TERM_NO AS TermNo FROM FINANCE_PAYMENT WHERE PKId = @PkId";
        var key = await _connection.QueryFirstOrDefaultAsync<(int BillId, byte TermNo)>(
            new CommandDefinition(getBillSql, new { PkId = paymentPkId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (key.BillId == 0) return;

        using var tx = _connection.BeginTransaction();
        try
        {
            await _connection.ExecuteAsync(new CommandDefinition("DELETE FROM FINANCE_PAYMENT WHERE PKId = @PkId", new { PkId = paymentPkId }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            const string reopenSql = @"UPDATE FINANCE_BILLS_TO_PAY_DETAIL SET STATUS = 'A' WHERE FINANCE_BILL_ID = @BillId AND TERM_NO = @TermNo";
            await _connection.ExecuteAsync(new CommandDefinition(reopenSql, new { key.BillId, key.TermNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
