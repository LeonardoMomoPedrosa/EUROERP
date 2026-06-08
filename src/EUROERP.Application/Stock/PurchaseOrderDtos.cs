namespace EUROERP.Application.Stock;

public class PurchaseOrderSupplierDto
{
    public int Id { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public byte StockDays { get; set; }
}

public class PurchaseSummaryDto
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public byte Days { get; set; }
}

public class PurchaseSupplierSummaryDto
{
    public int SupplierId { get; set; }
    public int PurchaseId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public bool Purchased { get; set; }
    public bool Received { get; set; }
    public decimal Amount { get; set; }
}

public class GeneratePurchaseRequestDto
{
    public byte Days { get; set; }
    public decimal DollarRate { get; set; } = 1;
    public decimal DollarVivoRate { get; set; } = 1;
    public IReadOnlyList<int> SupplierIds { get; set; } = Array.Empty<int>();
}

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderSupplierDto>> GetOrderingSuppliersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseSummaryDto>> GetLastPurchasesAsync(int count = 15, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseSupplierSummaryDto>> GetPurchaseSuppliersAsync(int purchaseId, CancellationToken cancellationToken = default);
    Task<int> GeneratePurchaseAsync(GeneratePurchaseRequestDto request, string userId, string applicationId, CancellationToken cancellationToken = default);
    Task SetPurchasedAsync(int purchaseId, int supplierId, bool purchased, bool cancelPending, CancellationToken cancellationToken = default);
}
