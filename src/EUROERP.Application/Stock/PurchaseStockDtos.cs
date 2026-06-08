namespace EUROERP.Application.Stock;

public class PurchaseStockPendingDto
{
    public int PurchaseId { get; set; }
    public int SupplierId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
}

public class PurchaseStockHistoryDto
{
    public int PurchaseId { get; set; }
    public int SupplierId { get; set; }
    public string DateOrder { get; set; } = string.Empty;
    public string DateIn { get; set; } = string.Empty;
    public string? TimeIn { get; set; }
    public int Days { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
}

public class PurchaseStockLineDto
{
    public int ProductId { get; set; }
    public string? ExternalCode { get; set; }
    public string? ExternalPkid { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Ordered { get; set; }
    public int Received { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal ConvertedPrice { get; set; }
}

public class PurchaseSupplierReceiptDto
{
    public string? ReceiptNo { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public decimal IcmsAmount { get; set; }
    public decimal ReceiptAmount { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal BoxAmount { get; set; }
    public decimal GtaAmount { get; set; }
    public decimal ShipAmount { get; set; }
    public string? Memo { get; set; }
}

public class PurchaseStockReceiveLineDto
{
    public int ProductId { get; set; }
    public int Received { get; set; }
    public decimal NewPrice { get; set; }
}

public class PurchaseStockSaveDto
{
    public int PurchaseId { get; set; }
    public int SupplierId { get; set; }
    public PurchaseSupplierReceiptDto Receipt { get; set; } = new();
    public IReadOnlyList<PurchaseStockReceiveLineDto> Lines { get; set; } = Array.Empty<PurchaseStockReceiveLineDto>();
}

public interface IPurchaseStockService
{
    Task<IReadOnlyList<PurchaseStockPendingDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseStockHistoryDto>> GetLastStockInsAsync(int rows = 15, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseStockLineDto>> GetReceiveLinesAsync(int purchaseId, int supplierId, CancellationToken cancellationToken = default);
    Task<PurchaseSupplierReceiptDto?> GetSupplierReceiptAsync(int purchaseId, int supplierId, CancellationToken cancellationToken = default);
    Task SaveReceiveDraftAsync(PurchaseStockSaveDto dto, CancellationToken cancellationToken = default);
    Task FinalizeReceiveAsync(PurchaseStockSaveDto dto, string userId, string applicationId, CancellationToken cancellationToken = default);
}
