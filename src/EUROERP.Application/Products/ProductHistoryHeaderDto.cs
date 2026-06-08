namespace EUROERP.Application.Products;

/// <summary>Product header for history result: name, size, current stock.</summary>
public class ProductHistoryHeaderDto
{
    public string Name { get; set; } = string.Empty;
    public string? Size { get; set; }
    public int Stock { get; set; }
}
