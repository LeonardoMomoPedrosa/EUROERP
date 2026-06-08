namespace EUROERP.Application.Clients;

public class ClientMassUpdateDto
{
    public int ClientId { get; set; }
    public decimal LimitAmount { get; set; }
    public byte AvgPayTerm { get; set; }
    public byte PaymentMethodId { get; set; }
    public byte PaymentMethodId2 { get; set; }
    public string? SalesAgentUserId { get; set; }
}
