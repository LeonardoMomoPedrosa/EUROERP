namespace EUROERP.Application.Stock;

public class StockAssetsBySupplierRowDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? LionCode { get; set; }
    public string? ProductName { get; set; }
    public decimal CostFinal { get; set; }
    public decimal Price { get; set; }
}

public interface IStockAssetsBySupplierService
{
    Task<IReadOnlyList<StockAssetsBySupplierRowDto>> GetBySupplierAsync(int supplierId, CancellationToken cancellationToken = default);
}
