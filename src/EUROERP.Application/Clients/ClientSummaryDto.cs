namespace EUROERP.Application.Clients;

/// <summary>DTO for client list row (search result).</summary>
public class ClientSummaryDto
{
    public int Id { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public string? FantasyName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Phone { get; set; }
    public string? FaxNo { get; set; }
    public string? Sales { get; set; }
}
