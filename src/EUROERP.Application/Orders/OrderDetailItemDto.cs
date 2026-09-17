namespace EUROERP.Application.Orders;

public class OrderDetailItemDto
{
    public int ProductId { get; set; }
    public byte Box { get; set; }
    public string LionCode { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Reserved/sent quantity (<c>ORDER_DETAILS.QUANTITY</c>). Zero on orçamentos until converted to venda.</summary>
    public decimal Quantity { get; set; }
    /// <summary>Requested quantity (<c>ORDER_DETAILS.QTD_ORDERED</c>).</summary>
    public decimal QtdOrdered { get; set; }
    /// <summary>True when the parent order is MODE=Q (orçamento).</summary>
    public bool IsQuote { get; set; }
    /// <summary>Cart/print quantity: requested qty on orçamento, reserved/sent qty on venda.</summary>
    public decimal DisplayQuantity => IsQuote ? QtdOrdered : Quantity;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsService { get; set; }
}
