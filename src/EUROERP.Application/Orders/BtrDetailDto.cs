namespace EUROERP.Application.Orders;

public class BtrDetailDto
{
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string Memo { get; set; } = "";
    public byte PaymentMethodId { get; set; }
}
