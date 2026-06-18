namespace EUROERP.Application.Orders;

/// <summary>
/// Data for the shipping label (etiqueta) print.
/// </summary>
public class OrderLabelDto
{
    public int OrderId { get; set; }
    public string MlOrderId { get; set; } = "";
    public string SocialName { get; set; } = "";
    public string AddressStreet { get; set; } = "";
    public string AddressNumber { get; set; } = "";
    public string AddressComplement { get; set; } = "";
    public string AddressBlock { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string AddressZipCode { get; set; } = "";
    public string Phone { get; set; } = "";
}
