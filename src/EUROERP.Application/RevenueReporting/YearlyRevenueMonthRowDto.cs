namespace EUROERP.Application.RevenueReporting;

/// <summary>One month row in the yearly revenue report.</summary>
public class YearlyRevenueMonthRowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    /// <summary>Display label e.g. "2025 / 1".</summary>
    public string MonthLabel { get; set; } = string.Empty;
    public int EnvCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DevAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal InternAmount { get; set; }
    public decimal LojaAmount { get; set; }
    public decimal BaixaAmount { get; set; }
    public decimal UsoAmount { get; set; }
    public decimal FpAmount { get; set; }
    public decimal MortMAmount { get; set; }
    public decimal MortDAmount { get; set; }
}
