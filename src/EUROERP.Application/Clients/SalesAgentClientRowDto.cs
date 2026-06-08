namespace EUROERP.Application.Clients;

public class SalesAgentClientRowDto
{
    public string SalesAgentUserName { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public string? FantasyName { get; set; }
    public string? Phone1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}
