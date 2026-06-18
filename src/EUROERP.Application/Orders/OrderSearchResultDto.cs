namespace EUROERP.Application.Orders;

/// <summary>DTO for order search result row (Consultar Pedido).</summary>
public class OrderSearchResultDto
{
    public int OrderId { get; set; }
    public DateTime CreationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClientFantasyName { get; set; } = string.Empty;
    public string? StateCode { get; set; }
    public string? CityName { get; set; }
}
