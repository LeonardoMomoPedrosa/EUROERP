namespace EUROERP.Application.RevenueReporting;

public class MonthlySupplierRevenueDayRowDto
{
    public string Date { get; set; } = string.Empty;
    public int Day { get; set; }
    public decimal Amount { get; set; }
    public decimal BaixaAmount { get; set; }
    public decimal UsoAmount { get; set; }
    public decimal FpAmount { get; set; }
}
