using EUROERP.Application.SalesReports;

namespace EUROERP.Web.Services;

/// <summary>
/// Circuit-scoped state for ABC / Minhas vendas (replaces legacy Session GroupDateRangeInfo + GROUP_REPORT).
/// </summary>
public class SalesReportStateService
{
    public DateRangeDto? DateRange { get; private set; }
    public SalesReportDataDto? ReportData { get; private set; }
    public string? SalesAgent { get; private set; }
    public bool CommissionMode { get; private set; }

    public void SetDateRange(DateTime firstDate, DateTime lastDate)
    {
        DateRange = new DateRangeDto { FirstDate = firstDate, LastDate = lastDate };
    }

    public void SetSalesAgent(string? salesAgent)
    {
        SalesAgent = string.IsNullOrWhiteSpace(salesAgent) ? null : salesAgent.Trim();
    }

    public void SetCommissionMode(bool commissionMode) => CommissionMode = commissionMode;

    public void SetReportData(SalesReportDataDto data) => ReportData = data;

    public bool HasData => ReportData != null;

    public void Clear()
    {
        DateRange = null;
        ReportData = null;
        SalesAgent = null;
        CommissionMode = false;
    }
}
