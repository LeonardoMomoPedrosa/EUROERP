namespace EUROERP.Application.NFe;

/// <summary>Order NFe detail for DANFEs page (search by order). Any order with NFE_PROTOCOL, regardless of status.</summary>
public class OrderNfeDetailForDanfeDto
{
    public int OrderId { get; set; }
    public string? ClientName { get; set; }
    public string? NfeKey { get; set; }
    public string? NfeProtocol { get; set; }
    public string? NfeReceipt { get; set; }
    public int? Receipt { get; set; }
    /// <summary>Relative path for PDF, e.g. "123/DFE44chars.pdf". Use with /NFE_FILES/ base.</summary>
    public string? PdfRelativePath { get; set; }
    /// <summary>Relative path for XML, e.g. "123/DFE44chars.xml". Use with /NFE_FILES/ base.</summary>
    public string? XmlRelativePath { get; set; }
    /// <summary>Protocolo de cancelamento (quando a nota foi cancelada).</summary>
    public string? NfeCancelProtocol { get; set; }
}
