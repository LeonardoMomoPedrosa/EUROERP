using System.Data;
using Dapper;
using EUROERP.Application.ClearCustomer;

namespace EUROERP.Infrastructure.ClearCustomer;

public class ClearCustomerService : IClearCustomerService
{
    private readonly IDbConnection _connection;

    public ClearCustomerService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<bool> IsClientDelinquentAsync(int clientId, CancellationToken cancellationToken = default)
    {
        // Overdue BTR details: DUE_DATE <= yesterday, STATUS = 'U', AMOUNT > 0
        const string sql = @"
            SELECT TOP 1
                ISNULL(c.ALLOW_DELINQ, GETDATE() - 2) AS AllowDelinq,
                c.IGNORE_DELINQ AS IgnoreDelinq
            FROM FINANCE_BTR btr
            JOIN FINANCE_BTR_DETAIL btrd ON btr.PKId = btrd.FINANCE_BTR_ID AND btrd.AMOUNT > 0
            JOIN CLIENT c ON btr.CLIENT_ID = c.PKId
            WHERE btr.CLIENT_ID = @ClientId
              AND btrd.STATUS = 'U'
              AND CAST(btrd.DUE_DATE AS DATE) <= CAST(DATEADD(day, -1, GETDATE()) AS DATE)
            ORDER BY btrd.DUE_DATE ASC";

        var row = await _connection.QuerySingleOrDefaultAsync<DelinqRow>(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));

        if (row == null || row.IgnoreDelinq)
            return false;

        var allowanceHours = DateTime.Today.DayOfWeek == DayOfWeek.Monday ? 72.0 : 24.0;
        var diff = DateTime.Today - row.AllowDelinq;
        return diff.TotalHours > allowanceHours;
    }

    public async Task AllowDelinquentClientAsync(int clientId, string userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE CLIENT
            SET ALLOW_DELINQ = GETDATE(),
                ALLOW_DELINQ_USER = @UserId,
                SYS_UPDATE_DATE = GETDATE()
            WHERE PKId = @ClientId";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { ClientId = clientId, UserId = userId ?? "SYS" }, cancellationToken: cancellationToken));
    }

    private sealed class DelinqRow
    {
        public DateTime AllowDelinq { get; init; }
        public bool IgnoreDelinq { get; init; }
    }
}
