namespace EUROERP.Application.Orders;

public class OrderPaymentSummaryDto
{
    public int OrderId { get; set; }
    public decimal Credit { get; set; }
    public decimal Discount { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal ShipmentCost { get; set; }
    public bool ChargeShipment { get; set; } = true;
    /// <summary>Sum of product lines (PRODUCT_CLASS_ID = 1), after line discount.</summary>
    public decimal ProductTotal { get; set; }
    /// <summary>Sum of service lines (PRODUCT_CLASS_ID = 2), after line discount.</summary>
    public decimal ServiceTotal { get; set; }
    /// <summary>ProductTotal + ServiceTotal (gross items before order credit/discount).</summary>
    public decimal ItemsSubtotal => ProductTotal + ServiceTotal;
    /// <summary>Legacy "Total parcial": items after order credit/discount, before OE/frete.</summary>
    public decimal PartialTotal { get; set; }
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
