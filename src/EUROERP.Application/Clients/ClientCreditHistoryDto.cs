namespace EUROERP.Application.Clients;

public class ClientCreditHistoryDto
{
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? OrderId { get; set; }
    public string? Memo { get; set; }
}
