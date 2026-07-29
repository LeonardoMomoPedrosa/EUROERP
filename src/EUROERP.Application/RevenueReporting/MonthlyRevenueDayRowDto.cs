namespace EUROERP.Application.RevenueReporting;

/// <summary>One day row in the monthly revenue report.</summary>
public class MonthlyRevenueDayRowDto
{
    public string Date { get; set; } = string.Empty; // dd/MM/yyyy
    public int Day { get; set; }
    public int EnvCount { get; set; }
    public decimal LojaAmount { get; set; }
    public decimal SiteAmount { get; set; }
    public decimal MeliAmount { get; set; }
    public decimal DevAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BaixaAmount { get; set; }
    public decimal UsoAmount { get; set; }
    public decimal FpAmount { get; set; }
    public decimal MortMAmount { get; set; }
    public decimal MortDAmount { get; set; }
}
