namespace EUROERP.Application.Products;

/// <summary>One product row for mass-cost grid (display and edit).</summary>
public class ProductMassCostItemDto
{
    public int PKId { get; set; }
    public string? Name { get; set; }
    public string? SciName { get; set; }
    public string? Size { get; set; }
    public string? Symbol { get; set; }
    public decimal CostGross { get; set; }
    public decimal Discount { get; set; }
    public decimal CostNet { get; set; }
    public decimal CostTransport { get; set; }
    public decimal CostFinal { get; set; }
    public decimal Profit { get; set; }
    public decimal Price { get; set; }
    public string? SymbolMkt { get; set; }
    public decimal Conversion { get; set; } = 1m;
    public decimal PriceMarket { get; set; }
    public short Stock { get; set; }
}
