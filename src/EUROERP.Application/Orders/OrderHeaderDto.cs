namespace EUROERP.Application.Orders;

public class OrderHeaderDto
{
    public int OrderId { get; set; }
    public string OrderType { get; set; } = "";
    /// <summary>S = Venda, Q = Orçamento (Eurobus ORDER.MODE).</summary>
    public string Mode { get; set; } = "S";
    public string ModeDescription { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "";
    public DateTime? SentDate { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = "";
    public string SocialName { get; set; } = "";
    public string SalesAgent { get; set; } = "";
    public string AddressStreet { get; set; } = "";
    public string AddressBlock { get; set; } = "";
    public string AddressZipCode { get; set; } = "";
    public string AddressNumber { get; set; } = "";
    public string AddressComplement { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public decimal LimitAmount { get; set; }
    public decimal DueAmount { get; set; }
    public decimal OrderTotalAmount { get; set; }
    public decimal BalanceForPurchase { get; set; }
    public decimal? Credit { get; set; }
    public decimal? Discount { get; set; }
    public decimal? OtherExpenses { get; set; }
    public decimal? ShipmentCost { get; set; }
    public bool ChargeShipment { get; set; } = true;
    public string Phone1 { get; set; } = "";
    public string Phone2 { get; set; } = "";
    public string Phone3 { get; set; } = "";
    public string Celular { get; set; } = "";
    public int? CarId { get; set; }
    public string? CarDescription { get; set; }
    public string? CarPlate { get; set; }
    public decimal? CarKm { get; set; }
    public string? CarProblem { get; set; }
    /// <summary>E-commerce site order id when the sale originated on the site.</summary>
    public int? SiteOrderId { get; set; }
    /// <summary>Mercado Livre order id when linked.</summary>
    public string? MlOrderId { get; set; }
}
