using System.Data;
using Dapper;
using EUROERP.Application.RevenueReporting;

namespace EUROERP.Infrastructure.RevenueReporting;

public class RevenueReportMonthlyService : IRevenueReportMonthlyService
{
    private readonly IDbConnection _connection;

    private const int CidBaixa = 275;
    private const int CidUso = 98;
    private const int CidFp = 76;
    private const int CidMortM = 74;
    private const int CidMortD = 4;

    public RevenueReportMonthlyService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<MonthlyRevenueResultDto> GetMonthlyRevenueReportAsync(MonthlyRevenueCriteriaDto criteria, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var month = criteria.Month;
        var year = criteria.Year;
        var firstDay = new DateTime(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var today = DateTime.Today;
        var endDate = lastDay > today ? today : lastDay;

        var param = new { MONTH = month, YEAR = year };

        const string sqlNormal = @"
SELECT t1.ORDER_TYPE AS OrderType, COUNT(t1.PKId) AS [Count], t1.SD AS Sd, t1.DAY AS [Day], SUM(t1.Amount) AS [To], SUM(t1.CRD) AS Crd
FROM (
    SELECT o.PKId, o.ORDER_TYPE, CONVERT(VARCHAR, o.SENT_DATE, 103) AS SD, DAY(o.SENT_DATE) AS [DAY], SUM(btrd.AMOUNT) AS Amount, o.CREDIT AS CRD
    FROM [ORDER] o
    JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
    JOIN [FINANCE_BTR] btr ON btr.PKId = o.BTR_ID
    JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
    WHERE o.STATUS = 'E' AND o.SALES_AGENT NOT IN ('SITE', 'MELI')
      AND MONTH(o.SENT_DATE) = @MONTH AND YEAR(o.SENT_DATE) = @YEAR
      AND c.LEDGE = 'Y'
    GROUP BY c.PKId, o.SENT_DATE, o.PKId, o.CREDIT, o.ORDER_TYPE
) AS t1
GROUP BY t1.SD, t1.DAY, t1.ORDER_TYPE
ORDER BY t1.SD";

        const string sqlSpecial = @"
SELECT t1.CLIENT_ID AS Cid, t1.SALES_AGENT AS Sa, t1.SD AS Sd, t1.DAY AS [Day], SUM(t1.Amount) AS [To], SUM(t1.CRD) AS Crd
FROM (
    SELECT c.PKId AS CLIENT_ID, o.SALES_AGENT, CONVERT(VARCHAR, o.SENT_DATE, 103) AS SD, DAY(o.SENT_DATE) AS [DAY], SUM(btrd.AMOUNT) AS Amount, SUM(o.CREDIT) AS CRD
    FROM [ORDER] o
    JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
    JOIN [FINANCE_BTR] btr ON btr.PKId = o.BTR_ID
    JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
    WHERE o.STATUS = 'E' AND (c.SPECIAL = 'Y' OR o.SALES_AGENT IN ('SITE', 'MELI'))
      AND MONTH(o.SENT_DATE) = @MONTH AND YEAR(o.SENT_DATE) = @YEAR
    GROUP BY c.PKId, o.SENT_DATE, c.FANTASY_NAME, o.SALES_AGENT
) AS t1
GROUP BY t1.SD, t1.CLIENT_ID, t1.SALES_AGENT, t1.DAY
ORDER BY t1.SD";

        const string sqlReturning = @"
SELECT CONVERT(VARCHAR, r.CREDIT_DATE, 103) AS Cd, DAY(r.CREDIT_DATE) AS [Day],
    SUM(CAST(ROUND(ROUND(ROUND(od.PRICE * od.CONVERSION, 2) * (1 - od.DISCOUNT / 100.0), 2), 2) * (1 - ISNULL(o.DISCOUNT, 0) / 100.0) AS DECIMAL(14,2)) * rod.QUANTITY) AS [To]
FROM [RETURN_ORDER_DETAILS] rod
JOIN [RETURN_ORDER] ro ON ro.ORDER_ID = rod.ORDER_ID
JOIN [RETURNING] r ON r.PKId = ro.RETURN_ID
JOIN [ORDER_DETAILS] od ON od.ORDER_ID = rod.ORDER_ID AND od.PRODUCT_ID = rod.PRODUCT_ID
JOIN [ORDER] o ON o.PKId = od.ORDER_ID
WHERE MONTH(r.CREDIT_DATE) = @MONTH AND YEAR(r.CREDIT_DATE) = @YEAR AND r.CREDIT_DATE IS NOT NULL
GROUP BY CONVERT(VARCHAR, r.CREDIT_DATE, 103), DAY(r.CREDIT_DATE)
ORDER BY [Day]";

        var cmd = new CommandDefinition(sqlNormal, param, cancellationToken: cancellationToken);
        var normalRows = (await _connection.QueryAsync<MonthNormalRow>(cmd)).ToList();

        var specialRows = (await _connection.QueryAsync<MonthSpecialRow>(new CommandDefinition(sqlSpecial, param, cancellationToken: cancellationToken))).ToList();

        List<ReturningByDayRow> returningRows;
        try
        {
            returningRows = (await _connection.QueryAsync<ReturningByDayRow>(new CommandDefinition(sqlReturning, param, cancellationToken: cancellationToken))).ToList();
        }
        catch
        {
            returningRows = new List<ReturningByDayRow>();
        }

        var dayDict = new Dictionary<string, MonthlyRevenueDayRowDto>();
        for (var d = firstDay; d <= endDate; d = d.AddDays(1))
        {
            var dateStr = d.ToString("dd/MM/yyyy");
            dayDict[dateStr] = new MonthlyRevenueDayRowDto
            {
                Date = dateStr,
                Day = d.Day
            };
        }

        foreach (var r in normalRows)
        {
            if (!dayDict.TryGetValue(r.Sd ?? "", out var row)) continue;
            row.EnvCount += r.Count;
            row.LojaAmount += r.To;
            row.CreditAmount += r.Crd;
        }

        foreach (var r in specialRows)
        {
            if (!dayDict.TryGetValue(r.Sd ?? "", out var row)) continue;
            var sa = (r.Sa ?? "").ToUpperInvariant();
            if (sa == "SITE") row.SiteAmount += r.To;
            else if (sa == "MELI") row.MeliAmount += r.To;
            if (r.Cid == CidBaixa) row.BaixaAmount += r.To;
            else if (r.Cid == CidUso) row.UsoAmount += r.To;
            else if (r.Cid == CidFp) row.FpAmount += r.To;
            else if (r.Cid == CidMortM) row.MortMAmount += r.To;
            else if (r.Cid == CidMortD) row.MortDAmount += r.To;
        }

        foreach (var r in returningRows)
        {
            if (!dayDict.TryGetValue(r.Cd ?? "", out var row)) continue;
            row.DevAmount += r.To;
        }

        var days = dayDict.Values.OrderBy(x => x.Day).ToList();

        return new MonthlyRevenueResultDto
        {
            Month = month,
            Year = year,
            Days = days,
            TotalLoja = days.Sum(x => x.LojaAmount),
            TotalSite = days.Sum(x => x.SiteAmount),
            TotalMeli = days.Sum(x => x.MeliAmount),
            TotalDev = days.Sum(x => x.DevAmount),
            TotalCredit = days.Sum(x => x.CreditAmount),
            TotalBaixa = days.Sum(x => x.BaixaAmount),
            TotalUso = days.Sum(x => x.UsoAmount),
            TotalFp = days.Sum(x => x.FpAmount),
            TotalMortM = days.Sum(x => x.MortMAmount),
            TotalMortD = days.Sum(x => x.MortDAmount)
        };
    }

    private class MonthNormalRow
    {
        public string? Sd { get; set; }
        public int Day { get; set; }
        public int Count { get; set; }
        public decimal To { get; set; }
        public decimal Crd { get; set; }
    }

    private class MonthSpecialRow
    {
        public int Cid { get; set; }
        public string? Sa { get; set; }
        public string? Sd { get; set; }
        public int Day { get; set; }
        public decimal To { get; set; }
        public decimal Crd { get; set; }
    }

    private class ReturningByDayRow
    {
        public string? Cd { get; set; }
        public int Day { get; set; }
        public decimal To { get; set; }
    }
}
