using System.ComponentModel.DataAnnotations;

namespace EUROERP.Application.Products;

/// <summary>Product data for create form.</summary>
public class ProductCreateDto
{
    public int GroupId { get; set; }
    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string Name { get; set; } = string.Empty;
    public string? SciName { get; set; }
    public string? ExternalPkId { get; set; }
    [Required(ErrorMessage = "Custo bruto é obrigatório.")]
    public decimal? CostGross { get; set; }
    [Required(ErrorMessage = "Custo transporte é obrigatório.")]
    public decimal? CostTransport { get; set; }
    [Required(ErrorMessage = "Desconto % é obrigatório.")]
    public decimal? Discount { get; set; }
    public int Weight { get; set; }
    public byte FiscalClassId { get; set; }
    public byte CurrencyId { get; set; }
    public byte CstId { get; set; }
    [Required(ErrorMessage = "CSTB é obrigatório.")]
    public string? CstbId { get; set; }
    public decimal? Ph { get; set; }
    public string? BarCode { get; set; }
    public short StockMin { get; set; }
    public int Pack { get; set; } = 1;
    public byte? SizeId { get; set; }
    public List<int> SupplierIds { get; set; } = new();
    /// <summary>Market id (default 1) and profit % for MARKET_PRODUCT.</summary>
    public byte MarketId { get; set; } = 1;
    public decimal ProfitPercent { get; set; }
}
