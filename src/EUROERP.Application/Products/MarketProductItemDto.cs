namespace EUROERP.Application.Products;

/// <summary>Lucro e preço do produto por mercado (MARKET_PRODUCT).</summary>
public class MarketProductItemDto
{
    public byte MarketId { get; set; }
    public string MarketName { get; set; } = string.Empty;
    public decimal ProfitPercent { get; set; }
    public decimal Price { get; set; }
}
