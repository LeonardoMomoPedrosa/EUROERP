namespace EUROERP.Application.CashFlow;

public class CashFlowResultDto
{
    public decimal OpenCashAmount { get; set; }
    public IReadOnlyList<CashFlowDayDto> Days { get; init; } = new List<CashFlowDayDto>();
}
