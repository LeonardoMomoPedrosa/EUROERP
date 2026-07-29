namespace EUROERP.Application.CashFlow;

public class CashFlowCriteriaDto
{
    public DateTime FirstDate { get; set; }
    public int Days { get; set; } = 30;
    public decimal OpenCashAmount { get; set; }
}
