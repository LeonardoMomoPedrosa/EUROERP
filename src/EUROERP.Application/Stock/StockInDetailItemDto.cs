namespace EUROERP.Application.Stock;

/// <summary>One line in the stock-in cart (STOCK_IN_DETAIL).</summary>
public class StockInDetailItemDto
{
    public int StockInId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public decimal UnitCostGross { get; set; }
    public decimal Ipi { get; set; }
    public decimal Discount { get; set; }
    public decimal UnitCostNet { get; set; }
    public decimal CostTransport { get; set; }
    public decimal UnitCostFinal { get; set; }
    public decimal TotalCost { get; set; }
    public decimal Profit { get; set; }
    public decimal UnitMarketPrice { get; set; }
    public string? CstbId { get; set; }
    public byte CurrencyId { get; set; }
    public decimal PreviousStock { get; set; }
}
