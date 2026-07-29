using System.Data;
using Dapper;
using EUROERP.Application.RevenueReporting;

namespace EUROERP.Infrastructure.RevenueReporting;

public class RevenueReportDailyService : IRevenueReportDailyService
{
    private readonly IDbConnection _connection;

    public RevenueReportDailyService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<DailyRevenueResultDto> GetDailyRevenueReportAsync(DailyRevenueCriteriaDto criteria, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string select = @"
SELECT o.PKId AS OrderId,
    CONVERT(VARCHAR, o.SENT_DATE, 103) AS SentDate,
    ISNULL(o.DISCOUNT, 0) AS Discount,
    ISNULL(o.CREDIT, 0) AS Credit,
    CONVERT(VARCHAR, o.SYS_CREATION_DATE, 103) AS OrderDate,
    c.PKId AS ClientId,
    c.SOCIAL_NAME AS SocialName,
    c.FANTASY_NAME AS FantasyName,
    ISNULL(cis.LEDGE, 'Y') AS Ledge,
    ISNULL(cis.CLIENT_ID, 0) AS SpecialClientId,
    btrd.PAYMENT_METHOD_ID AS PaymentMethodId,
    pm.NAME AS PaymentMethod,
    SUM(btrd.AMOUNT) AS Amount,
    CASE WHEN UPPER(RTRIM(o.SALES_AGENT)) = 'BOX_AGENT' THEN 'BOX' ELSE UPPER(RTRIM(o.SALES_AGENT)) END AS SalesAgent,
    ISNULL(o.RECEIPT, 0) AS Receipt,
    ISNULL(o.NFES_NO, 0) AS NfesNo,
    ISNULL((
        SELECT TOP 1 u.UserName
        FROM CLIENT_SALES_AGENTS_LINK sal
        INNER JOIN aspnet_Users u ON u.UserId = sal.USER_ID
        WHERE sal.CLIENT_ID = c.PKId
        ORDER BY u.UserName
    ), '') AS OfSaler
FROM [ORDER] o
JOIN FINANCE_BTR btr ON o.BTR_ID = btr.PKId
JOIN FINANCE_BTR_DETAIL btrd ON btr.PKId = btrd.FINANCE_BTR_ID
JOIN PAYMENT_METHOD pm ON pm.PKId = btrd.PAYMENT_METHOD_ID
JOIN CLIENT c ON c.PKId = o.CLIENT_ID
LEFT JOIN CLIENT_INV_SPECIAL cis ON cis.CLIENT_ID = c.PKId
WHERE o.STATUS = 'E'
  AND CAST(o.SENT_DATE AS DATE) >= @FIRST_DATE
  AND CAST(o.SENT_DATE AS DATE) <= @LAST_DATE";

        var where = new List<string>();
        var param = new Dictionary<string, object?>
        {
            ["FIRST_DATE"] = criteria.FirstDate.Date,
            ["LAST_DATE"] = criteria.LastDate.Date
        };

        if (criteria.PaymentMethodId > 0)
        {
            where.Add("btrd.PAYMENT_METHOD_ID = @PYM_METHOD");
            param["PYM_METHOD"] = criteria.PaymentMethodId;
        }
        if (!string.IsNullOrEmpty(criteria.SalesAgentName) && !criteria.SalesAgentName.Equals("Selecione", StringComparison.OrdinalIgnoreCase))
        {
            where.Add("o.SALES_AGENT = @SALES_AGENT");
            param["SALES_AGENT"] = criteria.SalesAgentName;
        }
        if (!string.IsNullOrEmpty(criteria.OfSalerName) && !criteria.OfSalerName.Equals("Selecione", StringComparison.OrdinalIgnoreCase))
        {
            where.Add(@"EXISTS (
                SELECT 1
                FROM CLIENT_SALES_AGENTS_LINK sal
                INNER JOIN aspnet_Users u ON u.UserId = sal.USER_ID
                WHERE sal.CLIENT_ID = c.PKId
                  AND u.UserName = @OF_SALER)");
            param["OF_SALER"] = criteria.OfSalerName;
        }
        if (criteria.SupplierId > 0)
        {
            where.Add(@"EXISTS (
                SELECT 1 FROM ORDER_DETAILS od
                JOIN PRODUCT_SUPPLIER_LINK psl ON psl.PRODUCT_ID = od.PRODUCT_ID
                WHERE od.ORDER_ID = o.PKId AND psl.SUPPLIER_ID = @SUPPLIER_ID)");
            param["SUPPLIER_ID"] = criteria.SupplierId;
        }

        var sql = select;
        if (where.Count > 0)
            sql += " AND " + string.Join(" AND ", where);
        sql += @"
GROUP BY o.PKId, btrd.PAYMENT_METHOD_ID, pm.NAME, c.PKId, c.SOCIAL_NAME, c.FANTASY_NAME,
  o.SYS_CREATION_DATE, o.CREDIT, o.SALES_AGENT, cis.LEDGE, cis.CLIENT_ID, o.DISCOUNT, o.SENT_DATE, o.RECEIPT, o.NFES_NO
ORDER BY o.PKId";

        var allRows = (await _connection.QueryAsync<DailyRevenueRowDto>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken))).ToList();

        var ledgeRows = allRows.Where(r => string.Equals(r.Ledge, "Y", StringComparison.OrdinalIgnoreCase)).ToList();

        var totalsByPm = ledgeRows
            .Where(r => !string.Equals(r.PaymentMethod, "MERCLIVRE", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.PaymentMethod ?? "")
            .OrderBy(g => g.Key)
            .Select(g => new PaymentMethodTotalDto
            {
                PaymentMethod = g.Key,
                Total = g.Sum(x => x.Amount)
            })
            .ToList();

        var totalLoja = ledgeRows
            .Where(r => !string.Equals(r.SalesAgent, "SITE", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(r.SalesAgent, "MELI", StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.Amount);

        var totalSite = ledgeRows
            .Where(r => string.Equals(r.SalesAgent, "SITE", StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.Amount);

        var totalMeli = ledgeRows
            .Where(r => string.Equals(r.SalesAgent, "MELI", StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.Amount);

        var total = ledgeRows.Sum(r => r.Amount);

        var specialGroups = ledgeRows
            .Where(r => r.SpecialClientId > 0 && r.SpecialClientId != 68)
            .GroupBy(r => r.SpecialClientId)
            .Select(g => new SpecialClientTotalDto
            {
                SocialName = g.First().SocialName ?? "",
                Total = g.Sum(r => r.Amount)
            })
            .OrderBy(x => x.SocialName)
            .ToList();

        return new DailyRevenueResultDto
        {
            Rows = ledgeRows,
            TotalsByPaymentMethod = totalsByPm,
            TotalLoja = totalLoja,
            TotalSite = totalSite,
            TotalMeli = totalMeli,
            Total = total,
            SpecialClientTotals = specialGroups
        };
    }
}
