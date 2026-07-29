namespace EUROERP.Application.AccountsReceivable;

public interface IBillsToReceiveReportService
{
    Task<ReceiveReportResultDto> GetReceiveReportAsync(ReceiveReportCriteriaDto criteria, CancellationToken cancellationToken = default);
}
