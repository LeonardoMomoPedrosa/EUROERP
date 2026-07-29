namespace EUROERP.Application.AccountsPayable;

/// <summary>One term (parcela) for creating a bill to pay.</summary>
public class BillsToPayTermDto
{
    public byte TermNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Memo { get; set; } = string.Empty;
}
