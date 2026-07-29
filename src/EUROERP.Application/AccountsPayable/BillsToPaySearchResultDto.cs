namespace EUROERP.Application.AccountsPayable;

/// <summary>Result of AP search: rows and criteria summary for display.</summary>
public class BillsToPaySearchResultDto
{
    public IReadOnlyList<BillsToPayReportRowDto> Rows { get; init; } = new List<BillsToPayReportRowDto>();
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
}
