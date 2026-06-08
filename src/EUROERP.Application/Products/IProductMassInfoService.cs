namespace EUROERP.Application.Products;

/// <summary>Service for mass-update product info (descriptions, no costs).</summary>
public interface IProductMassInfoService
{
    Task<IReadOnlyList<ProductMassInfoItemDto>> GetListForMassInfoAsync(ProductFilterDto filter, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateRowAsync(ProductMassInfoUpdateDto dto, CancellationToken cancellationToken = default);
}
