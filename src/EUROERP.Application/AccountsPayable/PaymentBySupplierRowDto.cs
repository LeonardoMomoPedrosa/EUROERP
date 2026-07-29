namespace EUROERP.Application.AccountsPayable;

public class PaymentBySupplierRowDto
{
    public int SupplierId { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
