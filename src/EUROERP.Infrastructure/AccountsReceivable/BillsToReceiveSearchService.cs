using System.Data;
using System.Globalization;
using Dapper;
using EUROERP.Application.AccountsReceivable;

namespace EUROERP.Infrastructure.AccountsReceivable;

public class BillsToReceiveSearchService : IBillsToReceiveSearchService
{
    private readonly IDbConnection _connection;

    public BillsToReceiveSearchService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<BillsToReceiveSearchResultDto> SearchAsync(BillsToReceiveSearchCriteriaDto criteria, CancellationToken cancellationToken = default)
    {
        var (sql, param) = BuildQuery(criteria);
        if (sql == null)
            return new BillsToReceiveSearchResultDto { FirstDate = DateTime.Today, LastDate = DateTime.Today };

        var rows = (await _connection.QueryAsync<BillsToReceiveReportRowDto>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken))).ToList();

        var firstDate = criteria.FirstDate ?? DateTime.Today;
        var lastDate = criteria.LastDate ?? DateTime.Today;
        if (rows.Count > 0 && criteria.OrderId == 0)
        {
            var withLedge = rows.Where(r => string.Equals(r.Ledge, "Y", StringComparison.OrdinalIgnoreCase)).ToList();
            if (withLedge.Count > 0)
            {
                var dates = withLedge.Select(r => r.DueDate).Where(d => !string.IsNullOrEmpty(d)).ToList();
                if (dates.Count > 0 && DateTime.TryParseExact(dates.Min(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fd))
                    firstDate = fd;
                if (dates.Count > 0 && DateTime.TryParseExact(dates.Max(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ld))
                    lastDate = ld;
            }
        }

        return new BillsToReceiveSearchResultDto
        {
            Rows = rows,
            FirstDate = firstDate,
            LastDate = lastDate
        };
    }

    private static (string? Sql, object? Param) BuildQuery(BillsToReceiveSearchCriteriaDto criteria)
    {
        // Eurobus FinancialBillsController.searchBtr — plus FantasyName; Paid/ComId via SUM (ERPCOM3).
        const string select = @"
SELECT o.PKId AS OrderId, o.BTR_ID AS BtrId, ISNULL(o.RECEIPT, 0) AS Receipt,
    ISNULL(o.NFES_NO, 0) AS NfesNo,
    btrd.TERM_NO AS TermNo, btr.TERMS AS Terms,
    ROUND(btrd.AMOUNT, 2) AS Amount,
    ROUND(ISNULL(SUM(fc.AMOUNT), 0), 2) AS Paid, ISNULL(SUM(fc.COMMISSION_ID), 0) AS ComId,
    CONVERT(VARCHAR, o.SYS_CREATION_DATE, 103) AS OrderDate,
    ISNULL(cis.LEDGE, 'Y') AS Ledge,
    CONVERT(VARCHAR, btrd.DUE_DATE, 103) AS DueDate,
    c.PKId AS ClientId, c.SOCIAL_NAME AS SocialName, c.FANTASY_NAME AS FantasyName,
    o.SALES_AGENT AS SalesAgent, pm.NAME AS PaymentMethod,
    ISNULL(psm.NAME, '') AS PaymentSubMethod
FROM [ORDER] o
JOIN [FINANCE_BTR] btr ON btr.PKId = o.BTR_ID
JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
JOIN [PAYMENT_METHOD] pm ON pm.PKId = btrd.PAYMENT_METHOD_ID
LEFT JOIN [PAYMENT_SUB_METHOD] psm ON psm.PKId = btrd.PAYMENT_SUB_METHOD_ID
LEFT JOIN [FINANCE_RECEIVE] fc ON fc.FINANCE_BTR_ID = btr.PKId AND fc.TERM_NO = btrd.TERM_NO
LEFT JOIN [CLIENT_INV_SPECIAL] cis ON cis.CLIENT_ID = c.PKId
LEFT JOIN [CLIENT_SALES_AGENTS_LINK] sal ON sal.CLIENT_ID = c.PKId
LEFT JOIN aspnet_Users au ON au.UserId = sal.USER_ID";

        var where = new List<string>();
        var param = new Dictionary<string, object?>();

        if (criteria.OrderId > 0)
        {
            where.Add("o.PKId = @OrderId");
            param["OrderId"] = criteria.OrderId;
        }
        else
        {
            if (!criteria.FirstDate.HasValue)
                return (null, null);

            var first = criteria.FirstDate.Value.Date;
            var last = (criteria.LastDate ?? criteria.FirstDate).Value.Date;
            param["FIRST_DATE"] = first;
            param["LAST_DATE"] = last;
            where.Add("CAST(btrd.DUE_DATE AS DATE) >= @FIRST_DATE AND CAST(btrd.DUE_DATE AS DATE) <= @LAST_DATE");

            if (criteria.PaymentMethodId > 0)
            {
                where.Add("btrd.PAYMENT_METHOD_ID = @PYM_METHOD");
                param["PYM_METHOD"] = criteria.PaymentMethodId;
            }

            if (!string.IsNullOrEmpty(criteria.Status) && criteria.Status != "0")
            {
                where.Add("btrd.STATUS = @STATUS");
                param["STATUS"] = criteria.Status;
            }

            if (!string.IsNullOrEmpty(criteria.SalesAgentName) && !string.Equals(criteria.SalesAgentName, "Selecione", StringComparison.OrdinalIgnoreCase))
            {
                where.Add("o.SALES_AGENT = @SALES_AGENT");
                param["SALES_AGENT"] = criteria.SalesAgentName;
            }

            if (!string.IsNullOrEmpty(criteria.OfSalerName) && !string.Equals(criteria.OfSalerName, "Selecione", StringComparison.OrdinalIgnoreCase))
            {
                where.Add("au.UserName = @OF_SALER");
                param["OF_SALER"] = criteria.OfSalerName;
            }

            if (criteria.ClientId > 0)
            {
                where.Add("o.CLIENT_ID = @CLIENT_ID");
                param["CLIENT_ID"] = criteria.ClientId;
            }
        }

        where.Add("o.STATUS = @ORDER_STATUS");
        param["ORDER_STATUS"] = string.IsNullOrEmpty(criteria.OrderStatus) ? "E" : criteria.OrderStatus;

        var sql = select + " WHERE " + string.Join(" AND ", where)
            + " GROUP BY o.PKId, o.BTR_ID, o.RECEIPT, o.NFES_NO, btrd.TERM_NO, btr.TERMS, btrd.AMOUNT, o.SYS_CREATION_DATE, btrd.DUE_DATE, c.PKId, c.SOCIAL_NAME, c.FANTASY_NAME, o.SALES_AGENT, pm.NAME, psm.NAME, cis.LEDGE"
            + " ORDER BY btrd.DUE_DATE ASC, c.SOCIAL_NAME, o.PKId, btrd.TERM_NO";

        return (sql, param);
    }
}
