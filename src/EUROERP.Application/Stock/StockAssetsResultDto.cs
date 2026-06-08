namespace EUROERP.Application.Stock;

public class StockAssetsResultDto
{
    public IReadOnlyList<StockAssetsRowDto> Rows { get; set; } = new List<StockAssetsRowDto>();
    public int CurrentLevel { get; set; }
    public string? Breadcrumb { get; set; }
    public decimal TotalStock { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalPrice { get; set; }
}
