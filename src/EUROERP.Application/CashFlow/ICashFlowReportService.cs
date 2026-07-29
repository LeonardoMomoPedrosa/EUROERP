namespace EUROERP.Application.CashFlow;

public interface ICashFlowReportService
{
    Task<CashFlowResultDto> GetCashFlowReportAsync(CashFlowCriteriaDto criteria, CancellationToken cancellationToken = default);
}
