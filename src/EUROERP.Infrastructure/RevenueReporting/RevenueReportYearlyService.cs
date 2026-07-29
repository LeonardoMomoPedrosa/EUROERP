using System.Data;
using Dapper;
using EUROERP.Application.RevenueReporting;

namespace EUROERP.Infrastructure.RevenueReporting;

public class RevenueReportYearlyService : IRevenueReportYearlyService
{
    private readonly IDbConnection _connection;

    private const int CidIntern = 68;
    private const int CidLoja = 18;
    private const int CidBaixa = 275;
    private const int CidUso = 98;
    private const int CidFp = 76;
    private const int CidMortM = 74;
    private const int CidMortD = 4;

    public RevenueReportYearlyService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<YearlyRevenueResultDto> GetYearlyRevenueReportAsync(YearlyRevenueCriteriaDto criteria, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var date1 = new DateTime(criteria.FirstYear, criteria.FirstMonth, 1);
        var date2 = new DateTime(criteria.LastYear, criteria.LastMonth, 1).AddMonths(1).AddDays(-1);

        var param = new { FIRST_DATE = date1, LAST_DATE = date2 };

        const string sqlNormal = @"
SELECT COUNT(t1.PKId) AS [Count], t1.MONTH AS [Month], t1.YEAR AS [Year], SUM(t1.AMOUNT) AS [To], SUM(t1.CRD) AS Crd
FROM (
    SELECT o.PKId, MONTH(o.SENT_DATE) AS MONTH, YEAR(o.SENT_DATE) AS YEAR, SUM(btrd.AMOUNT) AS AMOUNT, o.CREDIT AS CRD
    FROM [ORDER] o
    JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
    JOIN [FINANCE_BTR] btr ON btr.PKId = o.BTR_ID
    JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
    WHERE o.STATUS = 'E' AND o.SALES_AGENT NOT IN ('SITE', 'MELI')
      AND CAST(o.SENT_DATE AS DATE) >= @FIRST_DATE AND CAST(o.SENT_DATE AS DATE) <= @LAST_DATE
      AND c.LEDGE = 'Y'
    GROUP BY c.PKId, o.SENT_DATE, o.PKId, o.CREDIT
) AS t1
GROUP BY t1.MONTH, t1.YEAR
ORDER BY t1.YEAR, t1.MONTH";

        const string sqlSpecial = @"
SELECT t1.CLIENT_ID AS Cid, t1.YEAR AS [Year], t1.MONTH AS [Month], t1.SALES_AGENT AS Sa, SUM(t1.AMOUNT) AS [To]
FROM (
    SELECT c.PKId AS CLIENT_ID, MONTH(o.SENT_DATE) AS MONTH, YEAR(o.SENT_DATE) AS YEAR, o.SALES_AGENT, SUM(btrd.AMOUNT) AS AMOUNT
    FROM [ORDER] o
    JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
    JOIN [FINANCE_BTR] btr ON btr.PKId = o.BTR_ID
    JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
    WHERE o.STATUS = 'E'
      AND CAST(o.SENT_DATE AS DATE) >= @FIRST_DATE AND CAST(o.SENT_DATE AS DATE) <= @LAST_DATE
      AND (c.SPECIAL = 'Y' OR o.SALES_AGENT IN ('SITE', 'MELI'))
    GROUP BY c.PKId, o.SENT_DATE, o.SALES_AGENT, c.FANTASY_NAME
) AS t1
GROUP BY t1.CLIENT_ID, t1.SALES_AGENT, t1.YEAR, t1.MONTH
ORDER BY t1.YEAR, t1.MONTH";

        const string sqlReturning = @"
SELECT MONTH(r.CREDIT_DATE) AS [Month], YEAR(r.CREDIT_DATE) AS [Year],
    SUM(CAST(ROUND(ROUND(ROUND(od.PRICE * od.CONVERSION, 2) * (1 - od.DISCOUNT / 100.0), 2), 2) * (1 - ISNULL(o.DISCOUNT, 0) / 100.0) AS DECIMAL(14,2)) * rod.QUANTITY) AS [To]
FROM [RETURN_ORDER_DETAILS] rod
JOIN [RETURN_ORDER] ro ON ro.ORDER_ID = rod.ORDER_ID
JOIN [RETURNING] r ON r.PKId = ro.RETURN_ID
JOIN [ORDER_DETAILS] od ON od.ORDER_ID = rod.ORDER_ID AND od.PRODUCT_ID = rod.PRODUCT_ID
JOIN [ORDER] o ON o.PKId = od.ORDER_ID
WHERE r.STATUS = 'C' AND r.CREDIT_DATE IS NOT NULL
  AND CAST(r.CREDIT_DATE AS DATE) >= @FIRST_DATE AND CAST(r.CREDIT_DATE AS DATE) <= @LAST_DATE
GROUP BY MONTH(r.CREDIT_DATE), YEAR(r.CREDIT_DATE)
ORDER BY YEAR(r.CREDIT_DATE), MONTH(r.CREDIT_DATE)";

        var normalRows = (await _connection.QueryAsync<YearNormalRow>(new CommandDefinition(sqlNormal, param, cancellationToken: cancellationToken))).ToList();
        var specialRows = (await _connection.QueryAsync<YearSpecialRow>(new CommandDefinition(sqlSpecial, param, cancellationToken: cancellationToken))).ToList();

        List<ReturningByMonthRow> returningRows;
        try
        {
            returningRows = (await _connection.QueryAsync<ReturningByMonthRow>(new CommandDefinition(sqlReturning, param, cancellationToken: cancellationToken))).ToList();
        }
        catch
        {
            returningRows = new List<ReturningByMonthRow>();
        }

        var monthDict = new Dictionary<(int Year, int Month), YearlyRevenueMonthRowDto>();
        for (var d = date1; d.Year < date2.Year || (d.Year == date2.Year && d.Month <= date2.Month); d = d.AddMonths(1))
        {
            var key = (d.Year, d.Month);
            monthDict[key] = new YearlyRevenueMonthRowDto
            {
                Year = d.Year,
                Month = d.Month,
                MonthLabel = $"{d.Year} / {d.Month}"
            };
        }

        foreach (var r in normalRows)
        {
            if (!monthDict.TryGetValue((r.Year, r.Month), out var row)) continue;
            row.EnvCount += r.Count;
            row.TotalAmount += r.To;
            row.CreditAmount += r.Crd;
        }

        foreach (var r in specialRows)
        {
            if (!monthDict.TryGetValue((r.Year, r.Month), out var row)) continue;
            var sa = (r.Sa ?? "").ToUpperInvariant();
            if (r.Cid == CidIntern || sa == "SITE") row.InternAmount += r.To;
            if (r.Cid == CidLoja) row.LojaAmount += r.To;
            if (r.Cid == CidBaixa) row.BaixaAmount += r.To;
            if (r.Cid == CidUso) row.UsoAmount += r.To;
            if (r.Cid == CidFp) row.FpAmount += r.To;
            if (r.Cid == CidMortM) row.MortMAmount += r.To;
            if (r.Cid == CidMortD) row.MortDAmount += r.To;
        }

        foreach (var r in returningRows)
        {
            if (!monthDict.TryGetValue((r.Year, r.Month), out var row)) continue;
            row.DevAmount += r.To;
        }

        var months = monthDict.Values.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();

        return new YearlyRevenueResultDto
        {
            Months = months,
            TotalEnv = months.Sum(x => x.EnvCount),
            TotalAmount = months.Sum(x => x.TotalAmount),
            TotalDev = months.Sum(x => x.DevAmount),
            TotalCredit = months.Sum(x => x.CreditAmount),
            TotalIntern = months.Sum(x => x.InternAmount),
            TotalLoja = months.Sum(x => x.LojaAmount),
            TotalBaixa = months.Sum(x => x.BaixaAmount),
            TotalUso = months.Sum(x => x.UsoAmount),
            TotalFp = months.Sum(x => x.FpAmount),
            TotalMortM = months.Sum(x => x.MortMAmount),
            TotalMortD = months.Sum(x => x.MortDAmount)
        };
    }

    private class YearNormalRow
    {
        public int Count { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal To { get; set; }
        public decimal Crd { get; set; }
    }

    private class YearSpecialRow
    {
        public int Cid { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string? Sa { get; set; }
        public decimal To { get; set; }
    }

    private class ReturningByMonthRow
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal To { get; set; }
    }
}
