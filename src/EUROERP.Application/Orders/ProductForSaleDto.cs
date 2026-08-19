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
    /// <summary>True when PRODUCT_CLASS.PROD_SRV_IND = S (Eurobus: no stock check/deduction).</summary>
    public bool IsService { get; set; }
}
