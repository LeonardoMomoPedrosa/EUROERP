using System.Data;
using Dapper;
using EUROERP.Application.AccountsReceivable;

namespace EUROERP.Infrastructure.AccountsReceivable;

public class BillsToReceiveReceiveService : IBillsToReceiveReceiveService
{
    private readonly IDbConnection _connection;

    public BillsToReceiveReceiveService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<ReceiveRowDto>> GetReceivesAsync(int btrId, byte termNo, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"
SELECT PKId AS PkId, CONVERT(VARCHAR, SYS_CREATION_DATE, 103) AS Date,
    CONVERT(VARCHAR, SYS_CREATION_DATE, 108) AS Hour,
    ROUND(AMOUNT, 2) AS Amount, ISNULL(MEMO, '') AS Memo, USER_ID AS UserId,
    ISNULL(COMMISSION_ID, 0) AS ComId
FROM [FINANCE_RECEIVE]
WHERE FINANCE_BTR_ID = @BtrId AND TERM_NO = @TermNo
ORDER BY SYS_CREATION_DATE";
        var list = await _connection.QueryAsync<ReceiveRowDto>(
            new CommandDefinition(sql, new { BtrId = btrId, TermNo = termNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task RegisterReceiveAsync(int btrId, byte termNo, decimal amount, string? memo, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        applicationId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        userId = userId.Length > 20 ? userId[..20] : userId;
        memo = memo?.Length > 200 ? memo[..200] : (memo ?? "");

        const string leftSql = @"
SELECT ROUND(btrd.AMOUNT, 2) - ROUND(ISNULL(fr.Paid, 0), 2) AS LeftAmount
FROM [FINANCE_BTR_DETAIL] btrd
LEFT JOIN (SELECT FINANCE_BTR_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM [FINANCE_RECEIVE] GROUP BY FINANCE_BTR_ID, TERM_NO) fr
    ON fr.FINANCE_BTR_ID = btrd.FINANCE_BTR_ID AND fr.TERM_NO = btrd.TERM_NO
WHERE btrd.FINANCE_BTR_ID = @BtrId AND btrd.TERM_NO = @TermNo";
        var leftAmount = await _connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(leftSql, new { BtrId = btrId, TermNo = termNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (amount <= 0)
            throw new InvalidOperationException("Valor deve ser maior que zero.");
        if (decimal.Round(amount, 2) > decimal.Round(leftAmount, 2))
            throw new InvalidOperationException("Valor a receber não pode ser maior que o saldo.");

        using var tx = _connection.BeginTransaction();
        try
        {
            if (decimal.Round(amount, 2) == decimal.Round(leftAmount, 2))
            {
                const string closeSql = @"UPDATE [FINANCE_BTR_DETAIL] SET STATUS = 'P' WHERE FINANCE_BTR_ID = @BtrId AND TERM_NO = @TermNo";
                await _connection.ExecuteAsync(new CommandDefinition(closeSql, new { BtrId = btrId, TermNo = termNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            // Eurobus FINANCE_RECEIVE.TYPE default 'M' (manual)
            const string insertSql = @"
INSERT INTO [FINANCE_RECEIVE] (FINANCE_BTR_ID, TERM_NO, SYS_CREATION_DATE, USER_ID, APPLICATION_ID, AMOUNT, MEMO, TYPE, RETURN_ID)
VALUES (@BtrId, @TermNo, GETDATE(), @UserId, @ApplicationId, @Amount, @Memo, 'M', 0)";
            await _connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                BtrId = btrId,
                TermNo = termNo,
                UserId = userId,
                ApplicationId = applicationId,
                Amount = amount,
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

    public async Task CancelReceiveAsync(int receivePkId, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string getKeySql = @"SELECT FINANCE_BTR_ID AS BtrId, TERM_NO AS TermNo FROM [FINANCE_RECEIVE] WHERE PKId = @PkId";
        var key = await _connection.QueryFirstOrDefaultAsync<(int BtrId, byte TermNo)>(
            new CommandDefinition(getKeySql, new { PkId = receivePkId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (key.BtrId == 0) return;

        using var tx = _connection.BeginTransaction();
        try
        {
            await _connection.ExecuteAsync(new CommandDefinition("DELETE FROM [FINANCE_RECEIVE] WHERE PKId = @PkId", new { PkId = receivePkId }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            const string reopenSql = @"UPDATE [FINANCE_BTR_DETAIL] SET STATUS = 'U' WHERE FINANCE_BTR_ID = @BtrId AND TERM_NO = @TermNo";
            await _connection.ExecuteAsync(new CommandDefinition(reopenSql, new { key.BtrId, key.TermNo }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private void EnsureOpen()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }
}
