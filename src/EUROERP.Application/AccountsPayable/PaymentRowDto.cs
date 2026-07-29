namespace EUROERP.Application.AccountsPayable;

/// <summary>One payment row for (FINANCE_BILL_ID, TERM_NO).</summary>
public class PaymentRowDto
{
    public int PkId { get; set; }
    public string PaymentDate { get; set; } = string.Empty;
    public string SysCreationDate { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}
