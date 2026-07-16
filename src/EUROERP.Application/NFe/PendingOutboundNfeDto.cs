namespace EUROERP.Application.NFe;

/// <summary>Últimas Saídas — orders with product NFe (NFE_STATUS=1) and/or NFES.</summary>
public class PendingOutboundNfeDto
{
    public int OrderId { get; set; }
    public int? Receipt { get; set; }
    public string? NfeReceipt { get; set; }
    public string? NfeProtocol { get; set; }
    public string? NfeCancelProtocol { get; set; }
    public string? NfeKey { get; set; }
    public string? NfesNo { get; set; }
    public string SocialName { get; set; } = "";
}
