namespace EUROERP.Application.RevenueReporting;

public interface IRevenueReportDailyService
{
    Task<DailyRevenueResultDto> GetDailyRevenueReportAsync(DailyRevenueCriteriaDto criteria, CancellationToken cancellationToken = default);
}
