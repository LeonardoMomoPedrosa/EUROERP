namespace EUROERP.Application.SalesReports;

/// <summary>
/// XML for ranking_client_per_salers.xsl (&lt;DATA&gt; with DATES, Clients, Results).
/// </summary>
public class ClientSalesRankDataDto
{
    public string ReportXml { get; set; } = string.Empty;
    public string SalesAgent { get; set; } = string.Empty;
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
}
