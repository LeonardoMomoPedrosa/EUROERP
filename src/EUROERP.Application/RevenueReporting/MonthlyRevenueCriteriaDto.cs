namespace EUROERP.Application.RevenueReporting;

/// <summary>Criteria for monthly revenue (faturamento mensal) report.</summary>
public class MonthlyRevenueCriteriaDto
{
    public byte Month { get; set; }
    public int Year { get; set; }
}
