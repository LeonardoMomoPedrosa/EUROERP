namespace EUROERP.Application.Stock;

public class StockAssetsRowDto
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public int Stock { get; set; }
    public decimal CostUnit { get; set; }
    public decimal PriceUnit { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal MarginPercent { get; set; }
    public bool CanDrillDown { get; set; }
    public byte? ClassId { get; set; }
    public byte? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int Q1 { get; set; }
    public int Q2 { get; set; }
    public int Q3 { get; set; }
    public int Q4 { get; set; }
    public int Q5 { get; set; }
    public int Q6 { get; set; }
    public int Q7 { get; set; }
    public int Q8 { get; set; }
    public int Q9 { get; set; }
    public int Q10 { get; set; }
    public decimal Qm { get; set; }
}
