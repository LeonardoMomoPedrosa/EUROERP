namespace EUROERP.Application.Products;

/// <summary>Filter criteria for product list.</summary>
public class ProductFilterDto
{
    public byte MarketId { get; set; } = 1;
    public int ClassId { get; set; }
    public int GroupId { get; set; }
    public string? Name { get; set; }
    public string? SciName { get; set; }
    public int Code { get; set; }
    public int SupplierId { get; set; }
    public bool IncludeInactive { get; set; }
}
