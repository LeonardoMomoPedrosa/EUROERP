namespace EUROERP.Application.AccountsPayable;

public class PaymentByGroupRowDto
{
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
