namespace EUROERP.Application.AccountsReceivable;

/// <summary>One receive row for (FINANCE_BTR_ID, TERM_NO).</summary>
public class ReceiveRowDto
{
    public int PkId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Hour { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public long ComId { get; set; }
}
