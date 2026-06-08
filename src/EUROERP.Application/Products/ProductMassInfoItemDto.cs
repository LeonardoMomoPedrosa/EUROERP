namespace EUROERP.Application.Products;

/// <summary>One product row for mass-info grid (display and edit).</summary>
public class ProductMassInfoItemDto
{
    public int PKId { get; set; }
    public string? ExternalPkId { get; set; }
    public string Active { get; set; } = "Y"; // Y, N, X
    public bool Quarantine { get; set; }
    public int SupplierId { get; set; }
    public bool CFat { get; set; }
    public string? Name { get; set; }
    public string? SciName { get; set; }
    public string? MktName { get; set; }
    public string? MktSciName { get; set; }
    public byte? SizeId { get; set; }
    public decimal? Ph { get; set; }
    public byte CstId { get; set; }
    public string? CstbId { get; set; }
    public short StockMin { get; set; }
    public int Pack { get; set; }
    public short Stock { get; set; } // read-only
    /// <summary>Product class ID; used to show Quarentena only when != 4.</summary>
    public byte ProductClassId { get; set; }
}
