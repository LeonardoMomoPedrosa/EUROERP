namespace EUROERP.Application.RevenueReporting;

/// <summary>Criteria for yearly revenue (faturamento anual) report: date range by month/year.</summary>
public class YearlyRevenueCriteriaDto
{
    public byte FirstMonth { get; set; }
    public int FirstYear { get; set; }
    public byte LastMonth { get; set; }
    public int LastYear { get; set; }
}
