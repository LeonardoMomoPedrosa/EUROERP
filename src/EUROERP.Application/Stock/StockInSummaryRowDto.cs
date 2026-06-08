namespace EUROERP.Application.Stock;

/// <summary>Summary row for stock-in list (Consulta).</summary>
public class StockInSummaryRowDto
{
    public int Id { get; set; }
    public DateTime SysCreationDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ExternalPkid { get; set; }
    public decimal Cost { get; set; }
    public char Status { get; set; }
    public string? CurrencySymbol { get; set; }
}
