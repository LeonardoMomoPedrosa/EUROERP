using System.Data;
using Dapper;
using EUROERP.Application.RevenueReporting;

namespace EUROERP.Infrastructure.RevenueReporting;

/// <summary>Eurobus FinancialInvoicingController.getMonthInvoicingBySupplier + getMonthSpecialClientsInvoicingBySupplier.</summary>
public class RevenueReportMonthlySupplierService : IRevenueReportMonthlySupplierService
{
    private readonly IDbConnection _connection;

    private const int CidBaixa = 275;
    private const int CidUso = 98;
    private const int CidFp = 76;

    public RevenueReportMonthlySupplierService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<MonthlySupplierRevenueResultDto> GetMonthlySupplierRevenueReportAsync(MonthlySupplierRevenueCriteriaDto criteria, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var month = criteria.Month;
        var year = criteria.Year;
        var firstDay = new DateTime(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var today = DateTime.Today;
        var endDate = lastDay > today ? today : lastDay;

        var supplierName = await _connection.ExecuteScalarAsync<string>(
            new CommandDefinition(
                "SELECT SOCIAL_NAME FROM SUPPLIER WHERE PKId = @Id",
                new { Id = criteria.SupplierId },
                cancellationToken: cancellationToken)).ConfigureAwait(false) ?? "";

        var param = new { SUPPLIER_ID = criteria.SupplierId, MONTH = month, YEAR = year };

        // Eurobus line total from ORDER_DETAILS (not FINANCE_BTR)
        const string sqlNormal = @"
SELECT t1.SD AS Sd, t1.DAY AS [Day], SUM(t1.[TO]) AS [To]
FROM (
    SELECT CONVERT(VARCHAR, o.SENT_DATE, 103) AS SD,
        DAY(o.SENT_DATE) AS [DAY],
        ROUND(ROUND(ROUND(ROUND(od.PRICE * od.CONVERSION, 2) * (1 - od.DISCOUNT / 100.0), 2) * od.QUANTITY, 2)
            * (1 + ISNULL(o.DISCOUNT, 0) / 100.0 * (od.IGNORE_ORDER_DISC - 1)), 2) AS [TO]
    FROM ORDER_DETAILS od
    JOIN [ORDER] o ON o.PKId = od.ORDER_ID
    JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
    JOIN PRODUCT_SUPPLIER_LINK psl ON psl.PRODUCT_ID = od.PRODUCT_ID
    WHERE o.STATUS = 'E'
      AND psl.SUPPLIER_ID = @SUPPLIER_ID
      AND MONTH(o.SENT_DATE) = @MONTH AND YEAR(o.SENT_DATE) = @YEAR
      AND c.LEDGE = 'Y'
) AS t1
GROUP BY t1.SD, t1.DAY
ORDER BY t1.DAY";

        const string sqlSpecial = @"
SELECT t1.CLIENT_ID AS Cid, t1.SD AS Sd, t1.DAY AS [Day], SUM(t1.[TO]) AS [To]
FROM (
    SELECT o.CLIENT_ID,
        CONVERT(VARCHAR, o.SENT_DATE, 103) AS SD,
        DAY(o.SENT_DATE) AS [DAY],
        ROUND(ROUND(ROUND(ROUND(od.PRICE * od.CONVERSION, 2) * (1 - od.DISCOUNT / 100.0), 2) * od.QUANTITY, 2)
            * (1 + ISNULL(o.DISCOUNT, 0) / 100.0 * (od.IGNORE_ORDER_DISC - 1)), 2) AS [TO]
    FROM ORDER_DETAILS od
    JOIN [ORDER] o ON o.PKId = od.ORDER_ID
    JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
    JOIN PRODUCT_SUPPLIER_LINK psl ON psl.PRODUCT_ID = od.PRODUCT_ID
    WHERE o.STATUS = 'E'
      AND psl.SUPPLIER_ID = @SUPPLIER_ID
      AND MONTH(o.SENT_DATE) = @MONTH AND YEAR(o.SENT_DATE) = @YEAR
      AND c.SPECIAL = 'Y'
) AS t1
GROUP BY t1.CLIENT_ID, t1.SD, t1.DAY
ORDER BY t1.DAY";

        var normalRows = (await _connection.QueryAsync<SupplierDayRow>(
            new CommandDefinition(sqlNormal, param, cancellationToken: cancellationToken))).ToList();
        var specialRows = (await _connection.QueryAsync<SupplierSpecialRow>(
            new CommandDefinition(sqlSpecial, param, cancellationToken: cancellationToken))).ToList();

        var dayDict = new Dictionary<string, MonthlySupplierRevenueDayRowDto>();
        for (var d = firstDay; d <= endDate; d = d.AddDays(1))
        {
            var dateStr = d.ToString("dd/MM/yyyy");
            dayDict[dateStr] = new MonthlySupplierRevenueDayRowDto { Date = dateStr, Day = d.Day };
        }

        foreach (var r in normalRows)
        {
            if (!dayDict.TryGetValue(r.Sd ?? "", out var row)) continue;
            row.Amount += r.To;
        }

        foreach (var r in specialRows)
        {
            if (!dayDict.TryGetValue(r.Sd ?? "", out var row)) continue;
            if (r.Cid == CidBaixa) row.BaixaAmount += r.To;
            else if (r.Cid == CidUso) row.UsoAmount += r.To;
            else if (r.Cid == CidFp) row.FpAmount += r.To;
        }

        var days = dayDict.Values.OrderBy(x => x.Day).ToList();

        return new MonthlySupplierRevenueResultDto
        {
            SupplierId = criteria.SupplierId,
            SupplierName = supplierName,
            Month = month,
            Year = year,
            Days = days,
            TotalAmount = days.Sum(x => x.Amount),
            TotalBaixa = days.Sum(x => x.BaixaAmount),
            TotalUso = days.Sum(x => x.UsoAmount),
            TotalFp = days.Sum(x => x.FpAmount)
        };
    }

    private class SupplierDayRow
    {
        public string? Sd { get; set; }
        public int Day { get; set; }
        public decimal To { get; set; }
    }

    private class SupplierSpecialRow
    {
        public int Cid { get; set; }
        public string? Sd { get; set; }
        public int Day { get; set; }
        public decimal To { get; set; }
    }
}
