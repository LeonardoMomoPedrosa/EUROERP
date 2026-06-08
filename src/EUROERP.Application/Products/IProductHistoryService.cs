namespace EUROERP.Application.Products;

public interface IProductHistoryService
{
    Task<ProductHistoryHeaderDto?> GetHeaderAsync(int productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductHistoryEntryDto>> GetTimelineAsync(int productId, int days, CancellationToken cancellationToken = default);
}
