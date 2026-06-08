namespace EUROERP.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductSummaryDto>> GetListAsync(ProductFilterDto filter, CancellationToken cancellationToken = default);
    Task<ProductEditDto?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ProductCreateDto dto, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ProductEditDto dto, CancellationToken cancellationToken = default);
}
