namespace EUROERP.Application.Stock;

public class StockAssetsFilterDto
{
    public StockAssetsViewMode ViewMode { get; set; } = StockAssetsViewMode.Products;
    public int DrillLevel { get; set; }
    public byte? ClassId { get; set; }
    public byte? GroupId { get; set; }
}
