using System.ComponentModel.DataAnnotations;

namespace EUROERP.Application.Products;

/// <summary>Product data for edit form.</summary>
public class ProductEditDto
{
    public int PKId { get; set; }
    public byte ClassId { get; set; }
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SciName { get; set; }
    public string? ExternalPkId { get; set; }
    [Required(ErrorMessage = "Custo bruto é obrigatório.")]
    public decimal? CostGross { get; set; }
    [Required(ErrorMessage = "Custo transporte é obrigatório.")]
    public decimal? CostTransport { get; set; }
    [Required(ErrorMessage = "Desconto % é obrigatório.")]
    public decimal? Discount { get; set; }
    public decimal CostNet { get; set; }
    public decimal CostFinal { get; set; }
    public int? Weight { get; set; }
    public byte FiscalClassId { get; set; }
    public byte CurrencyId { get; set; }
    public byte CstId { get; set; }
    [Required(ErrorMessage = "CSTB é obrigatório.")]
    public string? CstbId { get; set; }
    public decimal? Ph { get; set; }
    public string? BarCode { get; set; }
    public short StockMin { get; set; }
    public int Pack { get; set; }
    public byte? SizeId { get; set; }
    public short Stock { get; set; }
    public DateTime? StockLastInDate { get; set; }
    public bool Active { get; set; }
    public bool Quarantine { get; set; }
    public List<int> SupplierIds { get; set; } = new();
    /// <summary>Margem de lucro e preço por mercado (MARKET_PRODUCT).</summary>
    public List<MarketProductItemDto> MarketProducts { get; set; } = new();
}
