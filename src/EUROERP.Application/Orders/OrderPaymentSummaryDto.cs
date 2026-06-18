namespace EUROERP.Application.Orders;

public class OrderPaymentSummaryDto
{
    public int OrderId { get; set; }
    public decimal Credit { get; set; }
    public decimal Discount { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal ShipmentCost { get; set; }
    public decimal TotalToPay { get; set; }
    public string CurrencySymbol { get; set; } = "";
    public string ClientName { get; set; } = "";
    public byte AvgPayterm { get; set; }
    public string Status { get; set; } = "";
    public int? BtrId { get; set; }
    public int ClientId { get; set; }
    public byte CurrencyId { get; set; }
    public string SalesAgent { get; set; } = "";
}
