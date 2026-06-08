namespace EUROERP.Application.Stock;

/// <summary>Header of a stock-in (STOCK_IN).</summary>
public class StockInHeaderDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? UserId { get; set; }
    public string ExternalPkid { get; set; } = string.Empty;
    public decimal? ShipCost { get; set; }
    public decimal? IiCost { get; set; }
    public decimal? IcmsCost { get; set; }
    public decimal? PisCost { get; set; }
    public decimal? CofinsCost { get; set; }
    public decimal? CreditAmount { get; set; }
    public bool NfeInd { get; set; }
    public decimal? NfeAmount { get; set; }
    public decimal ProductsCost { get; set; }
    public byte CurrencyId { get; set; } = 1;
    public char Status { get; set; } = 'N';

    public decimal ProrateTotal =>
        (ShipCost ?? 0) + (IiCost ?? 0) + (IcmsCost ?? 0) + (PisCost ?? 0) + (CofinsCost ?? 0) - (CreditAmount ?? 0);
}
