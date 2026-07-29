using System.Data;
using Dapper;
using EUROERP.Application.AccountsPayable;

namespace EUROERP.Infrastructure.AccountsPayable;

public class BillsToPayReportDailyService : IBillsToPayReportDailyService
{
    private readonly IDbConnection _connection;

    public BillsToPayReportDailyService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<BillsToPayByWeekRowDto>> GetByWeekAsync(DateTime firstDate, DateTime lastDate, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        await _connection.ExecuteAsync(new CommandDefinition("SET DATEFIRST 6", cancellationToken: cancellationToken)).ConfigureAwait(false);

        const string sql = @"
SELECT bpd.FINANCE_BILL_ID AS FinanceBillId, bpd.TERM_NO AS TermNo,
    DATEPART(week, bpd.DUE_DATE) AS [Week],
    DATEPART(weekday, bpd.DUE_DATE) AS WeekDay,
    CONVERT(VARCHAR, bpd.DUE_DATE, 103) AS DueDate,
    CONVERT(VARCHAR, bpd.DUE_DATE, 112) AS DueDateOrder,
    ROUND(bpd.AMOUNT, 2) AS Amount,
    ROUND(MAX(ISNULL(fp.Paid, 0)), 2) AS Paid,
    ROUND(bpd.AMOUNT * ISNULL(bp.CONVERSION, 1), 2) AS ConvertedAmount,
    ROUND(MAX(ISNULL(fp.Paid, 0)) * ISNULL(bp.CONVERSION, 1), 2) AS ConvertedPaid,
    bp.CURRENCY_ID AS CurrencyId
FROM FINANCE_BILLS_TO_PAY_DETAIL bpd
LEFT JOIN (SELECT FINANCE_BILL_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM FINANCE_PAYMENT GROUP BY FINANCE_BILL_ID, TERM_NO) fp
    ON fp.FINANCE_BILL_ID = bpd.FINANCE_BILL_ID AND fp.TERM_NO = bpd.TERM_NO
JOIN FINANCE_BILLS_TO_PAY bp ON bp.PKId = bpd.FINANCE_BILL_ID
JOIN SUPPLIER su ON su.PKId = bp.SUPPLIER_ID
JOIN SUPPLIER_GROUP sg ON sg.PKId = su.SUPPLIER_GROUP_ID
WHERE bpd.DUE_DATE >= @FIRST_DAY AND bpd.DUE_DATE <= @LAST_DAY
  AND (sg.HIDDEN IS NULL OR sg.HIDDEN = '')
GROUP BY bpd.DUE_DATE, bp.CURRENCY_ID, bpd.AMOUNT, bp.CONVERSION, bpd.FINANCE_BILL_ID, bpd.TERM_NO
ORDER BY bpd.DUE_DATE";

        var param = new { FIRST_DAY = firstDate.Date, LAST_DAY = lastDate.Date };
        var list = await _connection.QueryAsync<BillsToPayByWeekRowDto>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }
}
