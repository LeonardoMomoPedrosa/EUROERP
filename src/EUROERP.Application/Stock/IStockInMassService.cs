namespace EUROERP.Application.Stock;

public interface IStockInMassService
{
    Task<IReadOnlyList<StockMassProductRowDto>> GetSupplierProductsAsync(int supplierId, CancellationToken cancellationToken = default);
    Task ApplyMassStockAsync(int supplierId, string externalPkid, IReadOnlyList<StockMassApplyLineDto> lines, string userId, string applicationId, CancellationToken cancellationToken = default);
}
