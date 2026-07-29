namespace EUROERP.Application.AccountsPayable;

/// <summary>Supplier default payterm and payment method for AP create.</summary>
public class SupplierPaytermDto
{
    public byte? Payterm { get; set; }
    public byte? PaymentMethodId { get; set; }
}
