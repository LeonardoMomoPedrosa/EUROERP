namespace EUROERP.Application.Clients;

public class ClientMassItemDto
{
    public int Id { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public string? FantasyName { get; set; }
    public decimal LimitAmount { get; set; }
    public byte AvgPayTerm { get; set; }
    public byte PaymentMethodId { get; set; }
    public byte PaymentMethodId2 { get; set; }
    public string? SalesAgentUserName { get; set; }
    public string? SalesAgentUserId { get; set; }
}
