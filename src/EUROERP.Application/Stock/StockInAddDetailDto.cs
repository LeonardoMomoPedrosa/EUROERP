namespace EUROERP.Application.Stock;

/// <summary>Data to add one item to STOCK_IN_DETAIL.</summary>
public class StockInAddDetailDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCostGross { get; set; }
    public decimal Ipi { get; set; }
    public decimal Discount { get; set; }
    public decimal CostTransport { get; set; } = 1m;
    public decimal Profit { get; set; }
    public decimal UnitMarketPrice { get; set; }
    public string? CstbId { get; set; }
    public byte CurrencyId { get; set; } = 1;
}
