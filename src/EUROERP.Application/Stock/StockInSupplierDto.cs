namespace EUROERP.Application.Stock;

/// <summary>Supplier row for stock-in step 1 (selection).</summary>
public class StockInSupplierDto
{
    public int Id { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? City { get; set; }
}
