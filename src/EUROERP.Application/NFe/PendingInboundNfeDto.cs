namespace EUROERP.Application.NFe;

/// <summary>Últimas Entradas — RECEIPT_IN_DATA (NFe Outras).</summary>
public class PendingInboundNfeDto
{
    public int ReceiptNo { get; set; }
    public int? InternalReceipt { get; set; }
    public string? NfeReceipt { get; set; }
    public string? NfeProtocol { get; set; }
    public string? NfeCancelProtocol { get; set; }
    public string? NfeKey { get; set; }
    public string SocialName { get; set; } = "";
}
