namespace EUROERP.Application.RevenueReporting;

public interface IRevenueReportMonthlySupplierService
{
    Task<MonthlySupplierRevenueResultDto> GetMonthlySupplierRevenueReportAsync(MonthlySupplierRevenueCriteriaDto criteria, CancellationToken cancellationToken = default);
}
