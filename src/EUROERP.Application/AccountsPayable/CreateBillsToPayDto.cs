namespace EUROERP.Application.AccountsPayable;

/// <summary>Input for creating a manual bill to pay (Conta a Pagar).</summary>
public class CreateBillsToPayDto
{
    public int SupplierId { get; set; }
    public byte CurrencyId { get; set; }
    public byte PaymentMethodId { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public byte Terms { get; set; }
    public IReadOnlyList<BillsToPayTermDto> Details { get; set; } = new List<BillsToPayTermDto>();
}
