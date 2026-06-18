namespace EUROERP.Application.Orders;

public class PaymentMethodOptionDto
{
    public byte Id { get; set; }
    public string Name { get; set; } = "";
    public byte MaxTerms { get; set; }
    public decimal MinAmount { get; set; }
}
