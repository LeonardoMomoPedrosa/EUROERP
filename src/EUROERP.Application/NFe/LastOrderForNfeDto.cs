namespace EUROERP.Application.NFe;

public class LastOrderForNfeDto
{
    public int OrderId { get; set; }
    public string ClientFantasyName { get; set; } = "";
    public string? CityName { get; set; }
    public string? StateCode { get; set; }
    /// <summary>ORDER.RECEIPT - número do recibo/NF.</summary>
    public int? Receipt { get; set; }
    public string? NfeReceipt { get; set; }
    public string? NfeProtocol { get; set; }
    /// <summary>Protocolo de cancelamento (quando a NFe foi cancelada).</summary>
    public string? NfeCancelProtocol { get; set; }
}
