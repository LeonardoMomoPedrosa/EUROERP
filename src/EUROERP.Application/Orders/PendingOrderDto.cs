namespace EUROERP.Application.Orders;

/// <summary>DTO for "pedidos não enviados" list (pending orders, optional product filter).</summary>
public class PendingOrderDto
{
    public int OrderId { get; set; }
    public DateTime CreationDate { get; set; }
    public string ClientFantasyName { get; set; } = string.Empty;
    public string SalesAgent { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Total { get; set; }
}
