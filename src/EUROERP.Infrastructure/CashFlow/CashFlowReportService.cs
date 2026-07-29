using System.Globalization;
using EUROERP.Application.AccountsPayable;
using EUROERP.Application.AccountsReceivable;
using EUROERP.Application.CashFlow;

namespace EUROERP.Infrastructure.CashFlow;

/// <summary>
/// Eurobus cashflow_day_results — composes Contas a Receber + Contas a Pagar due-date searches.
/// </summary>
public class CashFlowReportService : ICashFlowReportService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IBillsToReceiveSearchService _arSearch;
    private readonly IBillsToPaySearchService _apSearch;

    public CashFlowReportService(IBillsToReceiveSearchService arSearch, IBillsToPaySearchService apSearch)
    {
        _arSearch = arSearch;
        _apSearch = apSearch;
    }

    public async Task<CashFlowResultDto> GetCashFlowReportAsync(CashFlowCriteriaDto criteria, CancellationToken cancellationToken = default)
    {
        var first = criteria.FirstDate.Date;
        var last = first.AddDays(Math.Max(1, criteria.Days) - 1);

        var arResult = await _arSearch.SearchAsync(new BillsToReceiveSearchCriteriaDto
        {
            FirstDate = first,
            LastDate = last,
            Status = "0",
            OrderStatus = "E"
        }, cancellationToken).ConfigureAwait(false);

        var apResult = await _apSearch.SearchAsync(new BillsToPaySearchCriteriaDto
        {
            FirstDate = first,
            LastDate = last,
            DueDateCriteria = true,
            Status = null
        }, cancellationToken).ConfigureAwait(false);

        var arByDate = arResult.Rows
            .GroupBy(r => r.DueDate)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BillsToReceiveReportRowDto>)g.ToList());

        var apByDate = apResult.Rows
            .GroupBy(r => r.DueDate)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BillsToPayReportRowDto>)g.ToList());

        var days = new List<CashFlowDayDto>();
        var balance = criteria.OpenCashAmount;

        for (var d = first; d <= last; d = d.AddDays(1))
        {
            var dateStr = d.ToString("dd/MM/yyyy", PtBr);
            arByDate.TryGetValue(dateStr, out var arRows);
            apByDate.TryGetValue(dateStr, out var apRows);
            arRows ??= Array.Empty<BillsToReceiveReportRowDto>();
            apRows ??= Array.Empty<BillsToPayReportRowDto>();

            // Previsto: full amount due that day (legacy cashflow uses AMOUNT / PAID; we use remaining balance for net flow)
            var receivableAmount = arRows.Sum(r => r.Amount);
            var payableAmount = apRows.Sum(r => r.ConvertedAmount);
            balance = balance + receivableAmount - payableAmount;

            days.Add(new CashFlowDayDto
            {
                Date = dateStr,
                ReceivableAmount = receivableAmount,
                PayableAmount = payableAmount,
                Balance = balance,
                Receivables = arRows,
                Payables = apRows
            });
        }

        return new CashFlowResultDto
        {
            OpenCashAmount = criteria.OpenCashAmount,
            Days = days
        };
    }
}
