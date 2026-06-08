namespace EUROERP.Application.Products;

/// <summary>Payload to update one product row in mass-cost screen.</summary>
public class ProductMassCostUpdateDto
{
    public int PKId { get; set; }
    public byte MarketId { get; set; }
    public decimal CostGross { get; set; }
    public decimal Discount { get; set; }
    public decimal CostTransport { get; set; }
    public decimal Profit { get; set; }
    public short Stock { get; set; }
}
