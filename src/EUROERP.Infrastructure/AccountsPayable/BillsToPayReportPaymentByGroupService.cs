using System.Data;
using Dapper;
using EUROERP.Application.AccountsPayable;

namespace EUROERP.Infrastructure.AccountsPayable;

public class BillsToPayReportPaymentByGroupService : IBillsToPayReportPaymentByGroupService
{
    private readonly IDbConnection _connection;

    public BillsToPayReportPaymentByGroupService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<PaymentByGroupRowDto>> GetPaymentsByGroupAsync(DateTime firstDate, DateTime lastDate, CancellationToken cancellationToken = default)
    {
        var span = (lastDate.Date - firstDate.Date).TotalDays;
        if (span > 400)
            throw new InvalidOperationException("Não é possível extrair relatórios com mais de 400 dias.");

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sql = @"
SELECT SUM(fp.AMOUNT * ISNULL(bp.CONVERSION, 1)) AS Amount, sg.NAME AS Name, sg.PKId AS GroupId
FROM FINANCE_PAYMENT fp
JOIN FINANCE_BILLS_TO_PAY bp ON bp.PKId = fp.FINANCE_BILL_ID
JOIN SUPPLIER su ON su.PKId = bp.SUPPLIER_ID
JOIN SUPPLIER_GROUP sg ON sg.PKId = su.SUPPLIER_GROUP_ID
WHERE fp.PAYMENT_DATE >= @FIRST_DATE AND fp.PAYMENT_DATE <= @LAST_DATE
GROUP BY sg.NAME, sg.PKId
ORDER BY SUM(fp.AMOUNT) DESC";

        var param = new { FIRST_DATE = firstDate.Date, LAST_DATE = lastDate.Date };
        var list = await _connection.QueryAsync<PaymentByGroupRowDto>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<IReadOnlyList<PaymentBySupplierRowDto>> GetPaymentsByGroupAndDateAsync(DateTime firstDate, DateTime lastDate, int groupId, CancellationToken cancellationToken = default)
    {
        var span = (lastDate.Date - firstDate.Date).TotalDays;
        if (span > 31)
            throw new InvalidOperationException("Não é possível extrair relatórios com mais de 31 dias.");

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sql = @"
SELECT SUM(fp.AMOUNT * ISNULL(bp.CONVERSION, 1)) AS Amount, su.SOCIAL_NAME AS SocialName, su.PKId AS SupplierId
FROM FINANCE_PAYMENT fp
JOIN FINANCE_BILLS_TO_PAY bp ON bp.PKId = fp.FINANCE_BILL_ID
JOIN SUPPLIER su ON su.PKId = bp.SUPPLIER_ID
WHERE fp.PAYMENT_DATE >= @FIRST_DATE AND fp.PAYMENT_DATE <= @LAST_DATE
  AND su.SUPPLIER_GROUP_ID = @SUPPLIER_GROUP_ID
GROUP BY su.SOCIAL_NAME, su.PKId
ORDER BY SUM(fp.AMOUNT) DESC";

        var param = new { FIRST_DATE = firstDate.Date, LAST_DATE = lastDate.Date, SUPPLIER_GROUP_ID = groupId };
        var list = await _connection.QueryAsync<PaymentBySupplierRowDto>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }
}
