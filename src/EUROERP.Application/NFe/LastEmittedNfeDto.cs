namespace EUROERP.Application.NFe;

/// <summary>Item for "Últimas Saídas" list on DANFEs page (orders with NFe authorized).</summary>
public class LastEmittedNfeDto
{
    public int OrderId { get; set; }
    /// <summary>ORDER.RECEIPT - número do recibo/NF.</summary>
    public int? Receipt { get; set; }
    public string? NfeReceipt { get; set; }
    public string? NfeProtocol { get; set; }
    public string? NfeCancelProtocol { get; set; }
    public string SocialName { get; set; } = "";
    /// <summary>Chave de acesso (44 chars). Used to build PDF/XML links from list if needed.</summary>
    public string? NfeKey { get; set; }
}
