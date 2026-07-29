namespace EUROERP.Application.RevenueReporting;

public interface IRevenueReportYearlyService
{
    Task<YearlyRevenueResultDto> GetYearlyRevenueReportAsync(YearlyRevenueCriteriaDto criteria, CancellationToken cancellationToken = default);
}
