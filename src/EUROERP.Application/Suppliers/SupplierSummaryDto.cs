namespace EUROERP.Application.Suppliers;

/// <summary>DTO for supplier list row (search result).</summary>
public class SupplierSummaryDto
{
    public int Id { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public string? City { get; set; }
}
