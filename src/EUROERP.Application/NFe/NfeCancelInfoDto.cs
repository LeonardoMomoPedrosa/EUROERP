namespace EUROERP.Application.NFe;

/// <summary>Info to cancel an NFe resolved by receipt number (sale or receipt-in).</summary>
public class NfeCancelInfoDto
{
    public bool IsSale { get; set; }
    public int OrderOrReceiptInId { get; set; }
    public string NfeKey { get; set; } = "";
    public string NfeProtocol { get; set; } = "";
    /// <summary>Folder name for NFE files: orderId or "IN" + receiptInNo.</summary>
    public string OrderIdName { get; set; } = "";
}
