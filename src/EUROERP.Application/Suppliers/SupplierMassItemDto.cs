namespace EUROERP.Application.Suppliers;

/// <summary>One row for supplier mass-update grid.</summary>
public class SupplierMassItemDto
{
    public int Id { get; set; }
    public string SocialName { get; set; } = string.Empty;
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
