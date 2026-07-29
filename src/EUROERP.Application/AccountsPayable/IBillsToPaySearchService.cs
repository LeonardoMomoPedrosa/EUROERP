namespace EUROERP.Application.AccountsPayable;

public interface IBillsToPaySearchService
{
    Task<BillsToPaySearchResultDto> SearchAsync(BillsToPaySearchCriteriaDto criteria, CancellationToken cancellationToken = default);
}

public interface ICreateBillsToPayService
{
    Task<int> CreateAsync(CreateBillsToPayDto dto, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task<SupplierPaytermDto?> GetSupplierPaytermAndPaymentMethodAsync(int supplierId, CancellationToken cancellationToken = default);
}
