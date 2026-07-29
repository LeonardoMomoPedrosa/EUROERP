namespace EUROERP.Application.SalesReports;

/// <summary>
/// XML parts for GROUP_REPORT (legacy Eurobus structure: GroupRefDs, CreditDs, NewDataSet).
/// </summary>
public class SalesReportDataDto
{
    public string GroupReportXml { get; set; } = string.Empty;
    public string CreditsReportXml { get; set; } = string.Empty;
    public string GroupRefReportXml { get; set; } = string.Empty;
}
