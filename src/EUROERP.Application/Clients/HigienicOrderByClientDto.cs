namespace EUROERP.Application.Clients;

/// <summary>Last hygiene service per fleet/car, grouped by client (Eurobus higienic_list_2).</summary>
public class HigienicOrderByClientDto
{
    public int OrderId { get; set; }
    public int CarId { get; set; }
    public int ClientId { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public string? CarDescription { get; set; }
    public string? CarPlate { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? RecommendDate { get; set; }
    public int DaysBehind { get; set; }
}
