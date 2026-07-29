namespace EUROERP.Application.RevenueReporting;

public interface IRevenueReportMonthlyService
{
    Task<MonthlyRevenueResultDto> GetMonthlyRevenueReportAsync(MonthlyRevenueCriteriaDto criteria, CancellationToken cancellationToken = default);
}
