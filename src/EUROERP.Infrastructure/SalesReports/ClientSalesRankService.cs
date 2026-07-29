using System.Data;
using System.Text;
using EUROERP.Application.SalesReports;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.SalesReports;

public class ClientSalesRankService : IClientSalesRankService
{
    private static readonly string[] ShortMonths =
    [
        "", "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"
    ];

    private readonly IDbConnection _connection;

    public ClientSalesRankService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<string>> GetSalesAgentsInVendasRoleAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT u.UserName
FROM aspnet_Users u
INNER JOIN aspnet_UsersInRoles ur ON u.UserId = ur.UserId
INNER JOIN aspnet_Roles r ON ur.RoleId = r.RoleId
WHERE r.LoweredRoleName = 'vendas'
ORDER BY u.UserName";

        var sqlConnection = (SqlConnection)_connection;
        if (sqlConnection.State != ConnectionState.Open)
            sqlConnection.Open();

        await using var cmd = new SqlCommand(sql, sqlConnection);
        var list = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            list.Add(reader.GetString(0));
        return list;
    }

    public async Task<ClientSalesRankDataDto> GetRankingAsync(string salesAgent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(salesAgent))
            throw new ArgumentException("Vendedor obrigatório.", nameof(salesAgent));

        var firstDate = DateTime.Today.AddMonths(-5);
        firstDate = new DateTime(firstDate.Year, firstDate.Month, 1);
        var lastDate = DateTime.Today;
        var dateRange = new DateRangeDto { FirstDate = firstDate, LastDate = lastDate };

        var sqlConnection = (SqlConnection)_connection;
        if (sqlConnection.State != ConnectionState.Open)
            sqlConnection.Open();

        var clientsDs = await GetClientListBySaleAgentAsync(sqlConnection, salesAgent, cancellationToken).ConfigureAwait(false);
        var detailsDs = await GetOfSalersInvoicingSummaryAsync(sqlConnection, dateRange, salesAgent, cancellationToken).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.Append("<DATA>");
        sb.Append("<DATES>");
        for (var i = firstDate; i <= lastDate; i = i.AddMonths(1))
        {
            sb.Append("<DATE>");
            sb.Append("<MONTH_NAME>");
            sb.Append(ShortMonths[i.Month]);
            sb.Append("</MONTH_NAME>");
            sb.Append("<MONTH>");
            sb.Append(i.Month);
            sb.Append("</MONTH>");
            sb.Append("<YEAR>");
            sb.Append(i.Year);
            sb.Append("</YEAR>");
            sb.Append("</DATE>");
        }
        sb.Append("</DATES>");
        sb.Append("<Clients>");
        sb.Append(clientsDs.GetXml());
        sb.Append("</Clients>");
        sb.Append("<Results>");
        sb.Append(detailsDs.GetXml());
        sb.Append("</Results>");
        sb.Append("</DATA>");

        return new ClientSalesRankDataDto
        {
            ReportXml = sb.ToString(),
            SalesAgent = salesAgent,
            FirstDate = firstDate,
            LastDate = lastDate
        };
    }

    private static async Task<DataSet> GetClientListBySaleAgentAsync(
        SqlConnection cnn,
        string saleAgent,
        CancellationToken cancellationToken)
    {
        const string query = @"
SELECT		t1.PKId,
            t1.FANTASY_NAME,
            t1.EMAIL,
            sum(t1.BAL) as BAL
FROM (
    SELECT		c.PKId,
                c.FANTASY_NAME,
                c.EMAIL,
                isnull(btrd.AMOUNT,0)-sum(isnull(fr.AMOUNT,0)) as BAL
    FROM		aspnet_Users au
    JOIN		CLIENT_SALES_AGENTS_LINK cl ON cl.User_Id = au.UserId
    JOIN		CLIENT c ON c.PKId = cl.CLIENT_ID
    LEFT JOIN	FINANCE_BTR btr ON btr.CLIENT_ID = c.PKId
    LEFT JOIN	FINANCE_BTR_DETAIL btrd ON btrd.FINANCE_BTR_ID = btr.PKId and btrd.DUE_DATE < getDate()
    LEFT JOIN	FINANCE_RECEIVE fr ON fr.FINANCE_BTR_ID = btrd.FINANCE_BTR_ID and fr.TERM_NO = btrd.TERM_NO
    WHERE		au.UserName = @SALES_AGENT
    GROUP BY	c.PKId,c.EMAIL,c.FANTASY_NAME,btrd.FINANCE_BTR_ID,btrd.AMOUNT,btrd.TERM_NO
) t1
GROUP BY t1.PKId,t1.FANTASY_NAME,t1.EMAIL
ORDER BY t1.FANTASY_NAME";

        await using var cmd = new SqlCommand(query, cnn);
        cmd.Parameters.AddWithValue("@SALES_AGENT", saleAgent);
        var adapter = new SqlDataAdapter(cmd);
        var ds = new DataSet();
        await Task.Run(() => adapter.Fill(ds, "ds"), cancellationToken).ConfigureAwait(false);
        return ds;
    }

    private static async Task<DataSet> GetOfSalersInvoicingSummaryAsync(
        SqlConnection cnn,
        DateRangeDto dateRange,
        string ofSaler,
        CancellationToken cancellationToken)
    {
        const string query = @"
SELECT t1.CID,t1.Y,t1.M,sum(t1.AMOUNT) as [TO] FROM (
    SELECT					c.PKid as CID,
                            year(o.SENT_DATE) as Y,
                            month(o.SENT_DATE) as M,
                            sum(btrd.AMOUNT) as AMOUNT
    FROM					CLIENT c
    LEFT JOIN				CLIENT_SALES_AGENTS_LINK sal ON c.PKId = sal.CLIENT_ID
    LEFT JOIN				aspnet_Users au ON sal.USER_ID = au.UserId
    LEFT JOIN				[ORDER] o ON c.PKId = o.CLIENT_ID
    JOIN					FINANCE_BTR btr ON o.BTR_ID = btr.PKId
    JOIN					FINANCE_BTR_DETAIL btrd ON btr.PKId = btrd.FINANCE_BTR_ID
    WHERE					o.STATUS = 'E'
    AND						c.LEDGE = 'Y'
    AND						o.SENT_DATE IS NOT NULL
    AND						o.SENT_DATE >= @FIRST_DATE
    AND						o.SENT_DATE < @LAST_DATE_END
    AND						au.UserName = @OFSALER_NAME
    GROUP BY				c.PKId,o.SENT_DATE
) t1
GROUP by t1.CID,t1.Y,t1.M";

        await using var cmd = new SqlCommand(query, cnn);
        cmd.Parameters.Add(new SqlParameter("@FIRST_DATE", SqlDbType.DateTime) { Value = dateRange.FirstDate.Date });
        cmd.Parameters.Add(new SqlParameter("@LAST_DATE_END", SqlDbType.DateTime) { Value = dateRange.LastDate.Date.AddDays(1) });
        cmd.Parameters.AddWithValue("@OFSALER_NAME", ofSaler);

        var adapter = new SqlDataAdapter(cmd);
        var ds = new DataSet();
        await Task.Run(() => adapter.Fill(ds, "ds"), cancellationToken).ConfigureAwait(false);
        return ds;
    }
}
