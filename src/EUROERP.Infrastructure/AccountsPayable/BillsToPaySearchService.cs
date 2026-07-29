using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;
using EUROERP.Application.AccountsPayable;

namespace EUROERP.Infrastructure.AccountsPayable;

public class BillsToPaySearchService : IBillsToPaySearchService
{
    private readonly IDbConnection _connection;

    public BillsToPaySearchService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<BillsToPaySearchResultDto> SearchAsync(BillsToPaySearchCriteriaDto criteria, CancellationToken cancellationToken = default)
    {
        var (sql, param) = BuildQuery(criteria);
        if (sql == null)
            return new BillsToPaySearchResultDto { FirstDate = DateTime.Today, LastDate = DateTime.Today };

        var rows = (await _connection.QueryAsync<BillsToPayReportRowDto>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken))).ToList();

        var firstDate = criteria.FirstDate ?? DateTime.Today;
        var lastDate = criteria.LastDate ?? DateTime.Today;
        if (rows.Count > 0 && string.IsNullOrWhiteSpace(criteria.IdStr))
        {
            var firstOrder = rows.Min(r => r.DueDateOrder);
            var lastOrder = rows.Max(r => r.DueDateOrder);
            if (DateTime.TryParseExact(firstOrder, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fd))
                firstDate = fd;
            if (DateTime.TryParseExact(lastOrder, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ld))
                lastDate = ld;
        }

        return new BillsToPaySearchResultDto
        {
            Rows = rows,
            FirstDate = firstDate,
            LastDate = lastDate
        };
    }

    private static (string? Sql, object? Param) BuildQuery(BillsToPaySearchCriteriaDto criteria)
    {
        const string select = @"
SELECT btp.PKId AS PkId, btp.SUPPLIER_ID AS SupplierId,
    CONVERT(VARCHAR, btp.SYS_CREATION_DATE, 103) AS SysCreationDate,
    pm.NAME AS PymMethod, pm.PKId AS PaymentMethodId, btp.TERMS AS Terms,
    btp.USER_ID AS UserId, btp.BILL_TYPE AS BillType, btp.STOCK_IN_ID AS StockInId,
    cu.SYMBOL AS Symbol, btp.CURRENCY_ID AS CurrencyId,
    ISNULL(btp.CONVERSION, 1) AS Conversion,
    su.SOCIAL_NAME AS SocialName,
    btpd.TERM_NO AS TermNo, btpd.STATUS AS Status,
    ROUND(btpd.AMOUNT, 2) AS Amount,
    ROUND(btpd.AMOUNT * ISNULL(btp.CONVERSION, 1), 2) AS ConvertedAmount,
    CONVERT(VARCHAR, btpd.DUE_DATE, 103) AS DueDate,
    CONVERT(VARCHAR, btpd.DUE_DATE, 112) AS DueDateOrder,
    ISNULL(btpd.MEMO, '--') AS Memo,
    ROUND(ISNULL(fp.Paid, 0), 2) AS Paid,
    ROUND(ISNULL(fp.Paid, 0) * ISNULL(btp.CONVERSION, 1), 2) AS ConvertedPaid,
    ISNULL(CONVERT(VARCHAR, btp.ORDER_DATE, 103), '--') AS OrderDate,
    bk.NAME AS Bank, sg.PKId AS Sgid, sg.HIDDEN AS Hidden
FROM FINANCE_BILLS_TO_PAY btp
JOIN FINANCE_BILLS_TO_PAY_DETAIL btpd ON btpd.FINANCE_BILL_ID = btp.PKId
JOIN PAYMENT_METHOD pm ON btp.PAYMENT_METHOD_ID = pm.PKId
JOIN CURRENCY cu ON cu.PKId = btp.CURRENCY_ID
JOIN SUPPLIER su ON su.PKId = btp.SUPPLIER_ID
JOIN SUPPLIER_GROUP sg ON su.SUPPLIER_GROUP_ID = sg.PKId
LEFT JOIN BANK bk ON su.BANK_INFO_BANK_ID = bk.PKId
LEFT JOIN (SELECT FINANCE_BILL_ID, TERM_NO, SUM(AMOUNT) AS Paid FROM FINANCE_PAYMENT GROUP BY FINANCE_BILL_ID, TERM_NO) fp
    ON fp.FINANCE_BILL_ID = btpd.FINANCE_BILL_ID AND fp.TERM_NO = btpd.TERM_NO";

        var where = new List<string>();
        var param = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(criteria.IdStr))
        {
            var ids = ParseIdList(criteria.IdStr);
            if (ids.Count == 0)
                return (null, null);
            where.Add($"btp.PKId IN ({string.Join(",", ids)})");
        }
        else
        {
            if (!criteria.FirstDate.HasValue)
                return (null, null);

            var first = criteria.FirstDate.Value.Date;
            var last = (criteria.LastDate ?? criteria.FirstDate).Value.Date;
            param["FIRST_DATE"] = first;
            param["LAST_DATE"] = last;

            if (criteria.DueDateCriteria)
                where.Add("CAST(btpd.DUE_DATE AS DATE) >= @FIRST_DATE AND CAST(btpd.DUE_DATE AS DATE) <= @LAST_DATE");
            else
                where.Add("btp.ORDER_DATE IS NOT NULL AND CAST(btp.ORDER_DATE AS DATE) >= @FIRST_DATE AND CAST(btp.ORDER_DATE AS DATE) <= @LAST_DATE");

            if (criteria.SupplierId > 0)
            {
                where.Add("btp.SUPPLIER_ID = @SUPPLIER_ID");
                param["SUPPLIER_ID"] = criteria.SupplierId;
            }

            if (criteria.SupplierGroupId > 0)
            {
                where.Add("sg.PKId = @SUPPLIER_GROUP_ID");
                param["SUPPLIER_GROUP_ID"] = criteria.SupplierGroupId;
            }

            where.Add("(sg.HIDDEN IS NULL OR sg.HIDDEN = '')");

            if (!string.IsNullOrEmpty(criteria.Status))
            {
                if (criteria.Status == "U")
                    where.Add("btpd.STATUS IN ('U', 'A')");
                else if (criteria.Status == "P")
                    where.Add("btpd.STATUS = 'P'");
                else if (criteria.Status == "N")
                    where.Add("btpd.STATUS = 'U'");
            }

            if (criteria.PaymentMethodId > 0)
            {
                where.Add("btp.PAYMENT_METHOD_ID = @PYM_METHOD");
                param["PYM_METHOD"] = criteria.PaymentMethodId;
            }
        }

        var sql = select + " WHERE " + string.Join(" AND ", where) + " ORDER BY btpd.DUE_DATE ASC, su.SOCIAL_NAME, btp.PKId, btpd.TERM_NO";
        return (sql, param);
    }

    /// <summary>Parse comma-separated IDs; only allow digits and commas (no SQL injection).</summary>
    private static IReadOnlyList<int> ParseIdList(string idStr)
    {
        if (string.IsNullOrWhiteSpace(idStr)) return Array.Empty<int>();
        var parts = idStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<int>();
        foreach (var p in parts)
        {
            if (Regex.IsMatch(p, @"^\d+$") && int.TryParse(p, out var id))
                list.Add(id);
        }
        return list;
    }
}
