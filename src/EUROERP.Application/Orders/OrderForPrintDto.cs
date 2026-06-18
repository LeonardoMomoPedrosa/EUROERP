namespace EUROERP.Application.Orders;

/// <summary>
/// Data for printing an order (types 1 = full, 2 = slip, 3 = packing slip).
/// </summary>
public class OrderForPrintDto
{
    public int OrderId { get; set; }
    public int ClientId { get; set; }
    public DateTime OrderDate { get; set; }
    public string ClientName { get; set; } = "";
    public string SocialName { get; set; } = "";
    public string CnpjPf { get; set; } = "";
    public string StateInscr { get; set; } = "";
    public string SalesAgent { get; set; } = "";
    public string AddressStreet { get; set; } = "";
    public string AddressNumber { get; set; } = "";
    public string AddressComplement { get; set; } = "";
    public string AddressBlock { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string AddressZipCode { get; set; } = "";
    public string Phone1 { get; set; } = "";
    public int? Receipt { get; set; }
    public string Memo { get; set; } = "";
    public string MlOrderId { get; set; } = "";
    public DateTime? MlOrderDate { get; set; }
    public decimal? MlShipmentCost { get; set; }
    public decimal Credit { get; set; }
    public decimal Discount { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal ShipmentCost { get; set; }
    public decimal Total { get; set; }
    public string CurrencySymbol { get; set; } = "";
    public IReadOnlyList<OrderForPrintLineDto> Details { get; set; } = Array.Empty<OrderForPrintLineDto>();
    /// <summary>Payment/BTR details (only for type 1).</summary>
    public IReadOnlyList<BtrDetailForPrintDto> PaymentDetails { get; set; } = Array.Empty<BtrDetailForPrintDto>();
}

public class OrderForPrintLineDto
{
    public string LionCode { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
}

public class BtrDetailForPrintDto
{
    public byte TermNo { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string? Memo { get; set; }
    public string PaymentMethodName { get; set; } = "";
}
