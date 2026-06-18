namespace EUROERP.Application.Orders;

public class ProductForSaleDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public int Stock { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public string CurrencySymbol { get; set; } = "";
    public decimal Conversion { get; set; }
    public bool CostInd { get; set; }
    public decimal CostFinal { get; set; }
}
