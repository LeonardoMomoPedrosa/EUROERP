using EUROERP.Application.RevenueReporting;
using EUROERP.Application.Widgets;
using Microsoft.Extensions.Caching.Memory;

namespace EUROERP.Infrastructure.Widgets;

/// <summary>Provides monthly revenue for the Daily Sales widget with 2-hour cache per month/year.</summary>
public sealed class WidgetDailySalesDataService : IWidgetDailySalesDataService
{
    private const string CacheKeyPrefix = "Widget:DailySales:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(2);

    private readonly IRevenueReportMonthlyService _monthlyService;
    private readonly IMemoryCache _cache;

    public WidgetDailySalesDataService(IRevenueReportMonthlyService monthlyService, IMemoryCache cache)
    {
        _monthlyService = monthlyService;
        _cache = cache;
    }

    public async Task<MonthlyRevenueResultDto> GetCurrentMonthRevenueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Today;
        var month = (byte)now.Month;
        var year = now.Year;
        var key = $"{CacheKeyPrefix}{month}:{year}";

        var result = await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var criteria = new MonthlyRevenueCriteriaDto { Month = month, Year = year };
            return await _monthlyService.GetMonthlyRevenueReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
        return result!;
    }
}
