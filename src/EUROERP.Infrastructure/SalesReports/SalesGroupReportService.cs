using System.Data;
using EUROERP.Application.SalesReports;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.SalesReports;

public class SalesGroupReportService : ISalesGroupReportService
{
    private readonly IDbConnection _connection;

    public SalesGroupReportService(IDbConnection connection)
    {
        _connection = connection;
    }

    public Task<SalesReportDataDto> GetAbcReportDataAsync(
        DateRangeDto dateRange,
        string? salesAgent = null,
        int clientId = 0,
        CancellationToken cancellationToken = default)
        => LoadAsync(dateRange, salesAgent, commissionInd: false, clientId, includeCredits: true, cancellationToken);

    public Task<SalesReportDataDto> GetMySalesReportDataAsync(
        DateRangeDto dateRange,
        string? salesAgent,
        int clientId = 0,
        CancellationToken cancellationToken = default)
        => LoadAsync(dateRange, salesAgent, commissionInd: true, clientId, includeCredits: false, cancellationToken);

    private async Task<SalesReportDataDto> LoadAsync(
        DateRangeDto dateRange,
        string? salesAgent,
        bool commissionInd,
        int clientId,
        bool includeCredits,
        CancellationToken cancellationToken)
    {
        EnsurePeriod(dateRange);

        var sqlConnection = (SqlConnection)_connection;
        if (sqlConnection.State != ConnectionState.Open)
            sqlConnection.Open();

        var groupDs = await GetProductGroupInvoicingAsync(sqlConnection, dateRange, salesAgent, commissionInd, clientId, cancellationToken)
            .ConfigureAwait(false);
        var groupRefDs = await GetProductGroupsAsync(sqlConnection, cancellationToken).ConfigureAwait(false);

        string creditsXml = "<CreditDs />";
        if (includeCredits)
        {
            var creditsDs = await GetOrderCreditsByPeriodAsync(sqlConnection, dateRange, cancellationToken).ConfigureAwait(false);
            creditsXml = DataSetToXml(creditsDs, "CreditDs");
        }

        return new SalesReportDataDto
        {
            GroupReportXml = DataSetToXml(groupDs, "NewDataSet"),
            CreditsReportXml = creditsXml,
            GroupRefReportXml = DataSetToXml(groupRefDs, "GroupRefDs")
        };
    }

    private static void EnsurePeriod(DateRangeDto dateRange)
    {
        var span = dateRange.LastDate.Date - dateRange.FirstDate.Date;
        if (span.TotalDays > 400)
            throw new InvalidOperationException("Não é possível extrair relatórios com mais de 1 ano");
    }

    private static string DataSetToXml(DataSet ds, string tableName)
    {
        var xml = ds.GetXml();
        return xml.Replace("NewDataSet", tableName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Port of Eurobus FinancialInvoicingController.getProductGroupInvoicing (Eurobus columns).
    /// </summary>
    private static async Task<DataSet> GetProductGroupInvoicingAsync(
        SqlConnection cnn,
        DateRangeDto dateRange,
        string? salesAgent,
        bool commissionInd,
        int aClientId,
        CancellationToken cancellationToken)
    {
        var query = @"
SELECT		o.PKId,
            convert(VARCHAR,SENT_DATE,103) as SD,
            day(o.SENT_DATE) as SDAY,
            od.PRODUCT_ID as PID,
            od.HAS_COST_IND as HCI,
            od.QUANTITY as QTD,
            isNull(od.WORKMAN,'?') as WORKMAN,
            p.NAME,
            p.CURRENCY_ID as CUID,
            round(round(od.PRICE,2)*od.QUANTITY*(1-od.DISCOUNT/100)*od.CONVERSION*(1+(isnull(o.DISCOUNT,0)*(isnull(od.IGNORE_ORDER_DISC,0)-1))/100),2) as PRICE,
            od.PRICE*(1-od.DISCOUNT/100)*(1+(isnull(o.DISCOUNT,0)*(isnull(od.IGNORE_ORDER_DISC,0)-1))/100) as PR,
            od.COST_FINAL*od.QUANTITY as CF,
            o.DISCOUNT as DSC,
            isnull(o.PAYMENT_SUB_METHOD_ID,0) as PSMID,
            isnull(psm.NAME,'nulo') as PSMN,
            pg.NAME as [GROUP],
            pg.PKId as GID,
            isNull(o.BTR_ID,0) as BID,
            c.PKId as CID,
            c.SOCIAL_NAME as SN,
            c.FANTASY_NAME as FN,
            UPPER(RTRIM(LTRIM(o.SALES_AGENT))) as SA,
            UPPER(isNull(au.UserName,'')) as OS,
            pg.PRODUCT_CLASS_ID as PC_ID
FROM [ORDER] o
JOIN CLIENT c ON c.PKId = o.CLIENT_ID
LEFT JOIN PAYMENT_SUB_METHOD psm ON psm.PKId = o.PAYMENT_SUB_METHOD_ID
LEFT JOIN CLIENT_SALES_AGENTS_LINK sal ON c.PKId = sal.CLIENT_ID
LEFT JOIN aspnet_Users au ON sal.USER_ID = au.UserId
LEFT JOIN ORDER_DETAILS od ON od.ORDER_ID = o.PKId
LEFT JOIN PRODUCT p ON p.PKId = od.PRODUCT_ID
JOIN PRODUCT_GROUP pg ON pg.PKId = p.GROUP_ID
WHERE o.STATUS = 'E'
AND c.LEDGE='Y'
" + (commissionInd ? "AND c.COMMISSION = 1 " : "AND c.DRE is null ")
+ @"AND od.QUANTITY > 0
AND o.SENT_DATE IS NOT NULL
AND o.SENT_DATE >= @FIRST_DATE
AND o.SENT_DATE < @LAST_DATE_END
"
+ (!string.IsNullOrEmpty(salesAgent) && salesAgent.Length > 1 ? "AND o.SALES_AGENT = @SALES_AGENT " : "")
+ (aClientId > 0 ? "AND c.PKId = @CLIENT_ID " : "")
+ @"
GROUP BY o.PKId,o.SENT_DATE,p.NAME,p.CURRENCY_ID,od.PRICE,o.BTR_ID,od.WORKMAN,od.HAS_COST_IND,od.COST_FINAL,od.QUANTITY,od.CONVERSION,o.DISCOUNT,o.PAYMENT_SUB_METHOD_ID,psm.NAME,c.SOCIAL_NAME,c.FANTASY_NAME,c.PKId,o.SYS_CREATION_DATE,
o.SALES_AGENT,au.UserName,od.PRODUCT_ID,od.DISCOUNT,pg.NAME,pg.PRODUCT_CLASS_ID,pg.PKId,od.IGNORE_ORDER_DISC
ORDER BY pg.PRODUCT_CLASS_ID desc";

        await using var cmd = new SqlCommand(query, cnn);
        cmd.Parameters.Add(new SqlParameter("@FIRST_DATE", SqlDbType.DateTime) { Value = dateRange.FirstDate.Date });
        cmd.Parameters.Add(new SqlParameter("@LAST_DATE_END", SqlDbType.DateTime) { Value = dateRange.LastDate.Date.AddDays(1) });
        if (!string.IsNullOrEmpty(salesAgent) && salesAgent.Length > 1)
            cmd.Parameters.AddWithValue("@SALES_AGENT", salesAgent);
        if (aClientId > 0)
            cmd.Parameters.AddWithValue("@CLIENT_ID", aClientId);

        var adapter = new SqlDataAdapter(cmd);
        var ds = new DataSet();
        await Task.Run(() => adapter.Fill(ds, "ds"), cancellationToken).ConfigureAwait(false);
        return ds;
    }

    private static async Task<DataSet> GetOrderCreditsByPeriodAsync(
        SqlConnection cnn,
        DateRangeDto dateRange,
        CancellationToken cancellationToken)
    {
        const string query = @"
SELECT		sum(o.CREDIT) as CREDIT
FROM		[ORDER] o
JOIN		[CLIENT] cli ON cli.PKId = o.CLIENT_ID
WHERE		o.SENT_DATE IS NOT NULL
AND			o.SENT_DATE >= @FIRST_DATE
AND			o.SENT_DATE < @LAST_DATE_END
AND			o.STATUS = 'E'
AND			o.BTR_ID is not null
AND			cli.LEDGE = 'Y'";

        await using var cmd = new SqlCommand(query, cnn);
        cmd.Parameters.Add(new SqlParameter("@FIRST_DATE", SqlDbType.DateTime) { Value = dateRange.FirstDate.Date });
        cmd.Parameters.Add(new SqlParameter("@LAST_DATE_END", SqlDbType.DateTime) { Value = dateRange.LastDate.Date.AddDays(1) });

        var adapter = new SqlDataAdapter(cmd);
        var ds = new DataSet();
        await Task.Run(() => adapter.Fill(ds, "ds"), cancellationToken).ConfigureAwait(false);
        return ds;
    }

    private static async Task<DataSet> GetProductGroupsAsync(SqlConnection cnn, CancellationToken cancellationToken)
    {
        const string query = @"
SELECT pg.PKId, pg.NAME, isNull(pg.IGNORE_ORDER_DISC,0) as IGNORE_ORDER_DISC, pc.NAME as CLASS, pc.PKId as CLASS_ID
FROM PRODUCT_GROUP pg, PRODUCT_CLASS pc
WHERE pg.PRODUCT_CLASS_ID = pc.PKId
ORDER BY CLASS, NAME";

        await using var cmd = new SqlCommand(query, cnn);
        var adapter = new SqlDataAdapter(cmd);
        var ds = new DataSet();
        await Task.Run(() => adapter.Fill(ds, "fc"), cancellationToken).ConfigureAwait(false);
        return ds;
    }
}
