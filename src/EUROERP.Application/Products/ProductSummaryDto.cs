namespace EUROERP.Application.Products;

/// <summary>DTO for product list row (search result).</summary>
public class ProductSummaryDto
{
    public int PKId { get; set; }
    public string? ExternalPkId { get; set; }
    public string? Name { get; set; }
    public string? SciName { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public string? CurrencySymbol { get; set; }
    public short Stock { get; set; }
    public short StockMin { get; set; }
    public DateTime? StockLastInDate { get; set; }
    public bool Quarantine { get; set; }
    public string? LionCode { get; set; }
}
