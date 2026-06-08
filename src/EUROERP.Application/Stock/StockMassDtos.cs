namespace EUROERP.Application.Stock;

public class StockMassProductRowDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ExternalPkid { get; set; }
    public decimal CostGross { get; set; }
    public decimal Discount { get; set; }
    public decimal Ipi { get; set; }
    public decimal Profit { get; set; }
    public byte CurrencyId { get; set; }
    public string? CstbId { get; set; }
    public bool DecimalInd { get; set; }
    public short Stock { get; set; }
}

public class StockMassApplyLineDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCostGross { get; set; }
    public decimal Ipi { get; set; }
    public decimal Discount { get; set; }
    public string? CstbId { get; set; }
    public decimal Profit { get; set; }
    public byte CurrencyId { get; set; }
    public bool DecimalInd { get; set; }
}
