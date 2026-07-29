namespace EUROERP.Application.AccountsReceivable;

/// <summary>Current BTR detail row for editing (alterar vencimento, valor, receber, etc.).</summary>
public class BillsToReceiveDetailDto
{
    public int BtrId { get; set; }
    public byte TermNo { get; set; }
    public byte Terms { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Left => Amount - Paid;
    public string Memo { get; set; } = string.Empty;
    public byte PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
}
