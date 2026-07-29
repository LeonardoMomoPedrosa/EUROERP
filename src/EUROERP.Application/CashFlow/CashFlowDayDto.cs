using EUROERP.Application.AccountsPayable;
using EUROERP.Application.AccountsReceivable;

namespace EUROERP.Application.CashFlow;

public class CashFlowDayDto
{
    public string Date { get; set; } = string.Empty;
    public decimal ReceivableAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal Balance { get; set; }
    public IReadOnlyList<BillsToReceiveReportRowDto> Receivables { get; init; } = new List<BillsToReceiveReportRowDto>();
    public IReadOnlyList<BillsToPayReportRowDto> Payables { get; init; } = new List<BillsToPayReportRowDto>();
}
