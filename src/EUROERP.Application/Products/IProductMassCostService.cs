namespace EUROERP.Application.Products;

/// <summary>Service for mass-update product costs and price.</summary>
public interface IProductMassCostService
{
    Task<IReadOnlyList<ProductMassCostItemDto>> GetListForMassCostAsync(ProductFilterDto filter, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateRowAsync(ProductMassCostUpdateDto dto, CancellationToken cancellationToken = default);
}
