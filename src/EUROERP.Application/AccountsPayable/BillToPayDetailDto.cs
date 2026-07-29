namespace EUROERP.Application.AccountsPayable;

/// <summary>Current detail row for editing (alterar vencimento, valor, memo, etc.).</summary>
public class BillToPayDetailDto
{
    public int BillId { get; set; }
    public byte TermNo { get; set; }
    public byte Terms { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Left => Amount - Paid;
    public string Memo { get; set; } = string.Empty;
    public byte PaymentMethodId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
