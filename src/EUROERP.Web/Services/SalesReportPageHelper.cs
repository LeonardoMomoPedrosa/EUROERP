using EUROERP.Application.SalesReports;

namespace EUROERP.Web.Services;

internal static class SalesReportPageHelper
{
    public static async Task EnsureAbcDataAsync(
        ISalesGroupReportService reportService,
        SalesReportStateService state,
        string? queryFirst,
        string? queryLast,
        CancellationToken cancellationToken = default)
    {
        if (state.DateRange == null
            && !string.IsNullOrEmpty(queryFirst) && !string.IsNullOrEmpty(queryLast)
            && DateTime.TryParse(queryFirst, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var qFirst)
            && DateTime.TryParse(queryLast, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var qLast))
        {
            state.SetDateRange(qFirst, qLast);
            state.SetCommissionMode(false);
        }

        if (state.DateRange == null || state.HasData)
            return;

        var data = state.CommissionMode
            ? await reportService.GetMySalesReportDataAsync(state.DateRange, state.SalesAgent, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await reportService.GetAbcReportDataAsync(state.DateRange, state.SalesAgent, cancellationToken: cancellationToken).ConfigureAwait(false);
        state.SetReportData(data);
    }
}
