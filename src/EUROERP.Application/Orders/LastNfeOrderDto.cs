namespace EUROERP.Application.Orders;

/// <summary>
/// Order with NFe key pending shipment (external API: last NFE orders for tracking).
/// </summary>
public class LastNfeOrderDto
{
    public int OrderId { get; set; }
    public int? ReceiptNo { get; set; }
    public string NfeKey { get; set; } = string.Empty;
    public string SocialName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
