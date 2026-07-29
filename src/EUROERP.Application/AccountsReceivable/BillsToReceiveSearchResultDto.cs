namespace EUROERP.Application.AccountsReceivable;

public class BillsToReceiveSearchResultDto
{
    public IReadOnlyList<BillsToReceiveReportRowDto> Rows { get; init; } = new List<BillsToReceiveReportRowDto>();
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
}
