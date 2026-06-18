namespace EUROERP.Application.Orders;

/// <summary>
/// Order and client data looked up by NFe key (Epic 14 external API).
/// </summary>
public class OrderByNfeKeyDto
{
    public string ClientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string SiteOrderId { get; set; } = string.Empty;
    public string MlOrderId { get; set; } = string.Empty;
}
