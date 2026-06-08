namespace EUROERP.Application.Stock;

public interface IStockInService
{
    Task<IReadOnlyList<StockInSupplierDto>> GetSuppliersForStockInAsync(string? name, CancellationToken cancellationToken = default);
    Task<(int? StockInId, string? Error)> CreateOrGetStockInAsync(StockInHeaderDto header, string userId, string applicationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockInSummaryRowDto>> GetStockInSummaryByDaysAsync(int days, CancellationToken cancellationToken = default);
    Task<StockInHeaderDto?> GetStockInHeaderAsync(int stockInId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockInDetailItemDto>> GetStockInDetailsAsync(int stockInId, CancellationToken cancellationToken = default);
    Task AddStockInDetailAsync(int stockInId, StockInAddDetailDto detail, CancellationToken cancellationToken = default);
    Task RemoveAllDetailsAsync(int stockInId, CancellationToken cancellationToken = default);
    Task RemoveStockInDetailAsync(int stockInId, int productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockInProductSearchDto>> GetProductSuggestionsAsync(string term, int? supplierId, int limit, CancellationToken cancellationToken = default);
    Task<StockInProductSearchDto?> GetProductForStockInAsync(int productId, CancellationToken cancellationToken = default);
    Task<decimal> CalculateTotalAsync(int stockInId, CancellationToken cancellationToken = default);
    Task FinalizeStockInAsync(int stockInId, string userId, string applicationId, IReadOnlyDictionary<int, string>? cstbByProductId = null, CancellationToken cancellationToken = default);
}
