namespace EUROERP.Application.Stock;

/// <summary>Product for stock-in autocomplete and add (cost/price for display).</summary>
public class StockInProductSearchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ExternalPkid { get; set; }
    public decimal CostGross { get; set; }
    public decimal CostNet { get; set; }
    public decimal Ipi { get; set; }
    public decimal Discount { get; set; }
    /// <summary>Custo transporte (multiplicador). Usado para Custo final = Custo líquido × Custo transporte.</summary>
    public decimal CostTransport { get; set; }
    public decimal CostFinal { get; set; }
    public decimal Price { get; set; }
    public decimal Stock { get; set; }
    public byte CurrencyId { get; set; }
    public string? CstbId { get; set; }
}
