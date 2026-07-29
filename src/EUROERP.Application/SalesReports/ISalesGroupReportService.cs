namespace EUROERP.Application.SalesReports;

public interface ISalesGroupReportService
{
    /// <summary>
    /// ABC report: commissionInd=false (DRE is null). Optional salesAgent / clientId filters.
    /// </summary>
    Task<SalesReportDataDto> GetAbcReportDataAsync(
        DateRangeDto dateRange,
        string? salesAgent = null,
        int clientId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Minhas vendas: commissionInd=true (COMMISSION=1). Empty salesAgent = all agents.
    /// </summary>
    Task<SalesReportDataDto> GetMySalesReportDataAsync(
        DateRangeDto dateRange,
        string? salesAgent,
        int clientId = 0,
        CancellationToken cancellationToken = default);
}
