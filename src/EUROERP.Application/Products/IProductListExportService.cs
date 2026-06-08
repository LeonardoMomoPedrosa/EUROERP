namespace EUROERP.Application.Products;

/// <summary>Eurobus product list (Geral) — PDF/Excel/HTML data. Legacy: product_list_search.aspx.</summary>
public interface IProductListExportService
{
    Task<ProductListExportDto> GetProductListDataAsync(ProductListRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfAsync(ProductListRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateExcelAsync(ProductListRequest request, CancellationToken cancellationToken = default);
}
