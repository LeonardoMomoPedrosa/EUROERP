namespace EUROERP.Application.Clients;

/// <summary>One row in the client discounts grid: product group and discount %.</summary>
public class ClientDiscountRowDto
{
    public byte ProductGroupId { get; set; }
    public string ProductGroupName { get; set; } = string.Empty;
    public decimal Discount { get; set; }
}
