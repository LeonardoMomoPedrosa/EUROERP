namespace EUROERP.Application.Orders;

public class OrderDetailItemDto
{
    public int ProductId { get; set; }
    public byte Box { get; set; }
    public string LionCode { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
}
