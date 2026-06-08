namespace EUROERP.Application.Products;

/// <summary>Payload to update one product row in mass-info (description) screen.</summary>
public class ProductMassInfoUpdateDto
{
    public int PKId { get; set; }
    public byte MarketId { get; set; }
    public string? ExternalPkId { get; set; }
    public string Active { get; set; } = "Y";
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
}
