namespace EUROERP.Application.Products;

/// <summary>Single timeline entry for product history (stock in/out).</summary>
public class ProductHistoryEntryDto
{
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Previous { get; set; }
    public decimal Quantity { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    /// <summary>True = entrada (quantity > 0), False = saída (quantity &lt; 0).</summary>
    public bool IsEntry => Quantity > 0;
}
