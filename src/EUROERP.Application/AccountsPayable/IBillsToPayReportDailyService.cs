namespace EUROERP.Application.AccountsPayable;

public interface IBillsToPayReportDailyService
{
    Task<IReadOnlyList<BillsToPayByWeekRowDto>> GetByWeekAsync(DateTime firstDate, DateTime lastDate, CancellationToken cancellationToken = default);
}
