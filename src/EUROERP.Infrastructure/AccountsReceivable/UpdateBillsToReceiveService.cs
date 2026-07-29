using System.Data;
using Dapper;
using EUROERP.Application.AccountsReceivable;

namespace EUROERP.Infrastructure.AccountsReceivable;

public class UpdateBillsToReceiveService : IUpdateBillsToReceiveService
{
    private readonly IDbConnection _connection;

    public UpdateBillsToReceiveService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<BillsToReceiveDetailDto?> GetDetailAsync(int btrId, byte termNo, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"
SELECT btr.PKId AS BtrId, btrd.TERM_NO AS TermNo, btr.TERMS AS Terms,
    btrd.DUE_DATE AS DueDate,
    ROUND(btrd.AMOUNT, 2) AS Amount,
    ROUND(ISNULL(fr.Paid, 0), 2) AS Paid,
    ISNULL(btrd.MEMO, '') AS Memo, btrd.PAYMENT_METHOD_ID AS PaymentMethodId,
    pm.NAME AS PaymentMethodName,
    c.SOCIAL_NAME AS ClientName, o.PKId AS OrderId, btrd.STATUS AS Status
FROM [FINANCE_BTR] btr
JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
JOIN [ORDER] o ON o.BTR_ID = btr.PKId
JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
JOIN [PAYMENT_METHOD] pm ON pm.PKId = btrd.PAYMENT_METHOD_ID
LEFT JOIN (SELECT FINANCE_BTR_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM [FINANCE_RECEIVE] GROUP BY FINANCE_BTR_ID, TERM_NO) fr
    ON fr.FINANCE_BTR_ID = btrd.FINANCE_BTR_ID AND fr.TERM_NO = btrd.TERM_NO
WHERE btr.PKId = @BtrId AND btrd.TERM_NO = @TermNo";
        return await _connection.QueryFirstOrDefaultAsync<BillsToReceiveDetailDto>(
            new CommandDefinition(sql, new { BtrId = btrId, TermNo = termNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpdateDueDateAsync(int btrId, byte termNo, DateTime dueDate, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        using var tx = _connection.BeginTransaction();
        try
        {
            await InsertBtrHistoryAsync(btrId, termNo, applicationId, userId, tx, cancellationToken).ConfigureAwait(false);
            const string sql = @"UPDATE [FINANCE_BTR_DETAIL] SET DUE_DATE = @DueDate WHERE FINANCE_BTR_ID = @BtrId AND TERM_NO = @TermNo";
            await _connection.ExecuteAsync(new CommandDefinition(sql, new { DueDate = dueDate, BtrId = btrId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdateAmountAsync(int btrId, byte termNo, DateTime dueDate, decimal amount, decimal paid, string memo, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        var status = (amount - paid) <= 0 ? "P" : "U";
        memo = memo?.Length > 200 ? memo[..200] : (memo ?? "");

        using var tx = _connection.BeginTransaction();
        try
        {
            await InsertBtrHistoryAsync(btrId, termNo, applicationId, userId, tx, cancellationToken).ConfigureAwait(false);
            // Eurobus updateBtr — no ORIG_AMOUNT column on FINANCE_BTR_DETAIL
            const string sql = @"UPDATE [FINANCE_BTR_DETAIL] SET DUE_DATE = @DueDate, AMOUNT = @Amount, STATUS = @Status, MEMO = @Memo
WHERE FINANCE_BTR_ID = @BtrId AND TERM_NO = @TermNo";
            await _connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                DueDate = dueDate,
                Amount = amount,
                Status = status,
                Memo = memo,
                BtrId = btrId,
                TermNo = termNo
            }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task UpdatePaymentMethodAsync(int btrId, byte termNo, byte paymentMethodId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"UPDATE [FINANCE_BTR_DETAIL] SET PAYMENT_METHOD_ID = @PaymentMethodId WHERE FINANCE_BTR_ID = @BtrId AND TERM_NO = @TermNo";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { PaymentMethodId = paymentMethodId, BtrId = btrId, TermNo = termNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> HasPaymentAsync(int btrId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"SELECT 1 FROM [FINANCE_RECEIVE] WHERE FINANCE_BTR_ID = @BtrId";
        var exists = await _connection.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { BtrId = btrId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return exists.HasValue && exists.Value == 1;
    }

    /// <summary>Eurobus FinancialBillsController.insertBtrHistory — snapshot before change.</summary>
    private async Task InsertBtrHistoryAsync(int btrId, byte termNo, string applicationId, string userId, IDbTransaction tx, CancellationToken cancellationToken)
    {
        applicationId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        userId = userId.Length > 20 ? userId[..20] : userId;

        const string sql = @"
INSERT INTO [FINANCE_BTR_CHG_HST]
    (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, FINANCE_BTR_ID, TERM_NO, AMOUNT, STATUS, MEMO, DUE_DATE)
SELECT GETDATE(), @UserId, @ApplicationId, btrd.FINANCE_BTR_ID, btrd.TERM_NO, btrd.AMOUNT, btrd.STATUS, btrd.MEMO, btrd.DUE_DATE
FROM [FINANCE_BTR_DETAIL] btrd
WHERE btrd.FINANCE_BTR_ID = @BtrId AND btrd.TERM_NO = @TermNo";

        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            ApplicationId = applicationId,
            BtrId = btrId,
            TermNo = termNo
        }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private void EnsureOpen()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }
}
