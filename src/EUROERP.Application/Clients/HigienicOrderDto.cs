namespace EUROERP.Application.Clients;

public class HigienicOrderDto
{
    public int OrderId { get; set; }
    public DateTime? SentDate { get; set; }
    public string? SalesAgent { get; set; }
    public int? NfeNo { get; set; }
    public int ClientId { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? CarDescription { get; set; }
    public string? CarPlate { get; set; }
    public long? CarKm { get; set; }
    public string? CarProblem { get; set; }
}
