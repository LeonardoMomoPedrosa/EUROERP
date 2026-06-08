namespace EUROERP.Application.Suppliers;

/// <summary>Payload to update one supplier row in mass-update.</summary>
public class SupplierMassUpdateDto
{
    public int Id { get; set; }
    public int SupplierGroupId { get; set; }
    public byte? PaymentMethodId { get; set; }
    public int? Payterm { get; set; }
    public string? PaymentPlan { get; set; }
    public int? StockDays { get; set; }
    public bool GnrlOrdering { get; set; }
    public int? OperationPerc { get; set; }
    public int? AdminPerc { get; set; }
    public int? SalesPerc { get; set; }
}
