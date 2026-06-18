namespace EUROERP.Application.Orders;

/// <summary>
/// Client and tracking data for an order (Epic 14 external API).
/// </summary>
public class ClientByOrderDto
{
    public string ClientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Track { get; set; } = string.Empty;
    public string Via { get; set; } = string.Empty;
    public string NfeKey { get; set; } = string.Empty;
    public string Receipt { get; set; } = string.Empty;
}
