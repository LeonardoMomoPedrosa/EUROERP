namespace EUROERP.Application.AccountsReceivable;

public interface IBillsToReceiveSearchService
{
    Task<BillsToReceiveSearchResultDto> SearchAsync(BillsToReceiveSearchCriteriaDto criteria, CancellationToken cancellationToken = default);
}
