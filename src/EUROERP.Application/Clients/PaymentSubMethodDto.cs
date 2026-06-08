namespace EUROERP.Application.Clients;

/// <summary>Payment sub-method row (legacy ReferenceService.getPaymentSubMethods / <c>PAYMENT_SUB_METHOD</c>).</summary>
public sealed class PaymentSubMethodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PaymentMethodId { get; set; }
    public byte? MaxTerms { get; set; }
    public decimal? MinAmount { get; set; }
    public bool AllowFront { get; set; }
}
