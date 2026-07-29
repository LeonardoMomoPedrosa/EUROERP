using EUROERP.Application.RevenueReporting;

namespace EUROERP.Application.Widgets;

/// <summary>Provides monthly revenue data for the Daily Sales widget. Results are cached for 2 hours per month/year.</summary>
public interface IWidgetDailySalesDataService
{
    /// <summary>Gets monthly revenue for the current month/year (cached 2h). Used by the Daily Sales dashboard widget.</summary>
    Task<MonthlyRevenueResultDto> GetCurrentMonthRevenueAsync(CancellationToken cancellationToken = default);
}
