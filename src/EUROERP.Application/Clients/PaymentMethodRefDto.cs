namespace EUROERP.Application.Clients;

/// <summary>Payment method row for SL4Box / POS reference (legacy <c>getPaymentMethods</c> / <c>getPaymentMethod</c> columns on <c>PAYMENT_METHOD</c>).</summary>
public sealed class PaymentMethodRefDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte MaxTerms { get; set; }
    public decimal MinAmount { get; set; }
}
