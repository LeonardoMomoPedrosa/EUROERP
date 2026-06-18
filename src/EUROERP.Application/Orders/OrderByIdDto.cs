namespace EUROERP.Application.Orders;

/// <summary>External API: order summary by id (dates, status, NFe key).</summary>
public class OrderByIdDto
{
    public DateTime? OrderDate { get; set; }
    public DateTime? SentDate { get; set; }
    public string Status { get; set; } = "";
    public string NfeKey { get; set; } = "";
}
