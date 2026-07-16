namespace EUROERP.Application.NFe;

/// <summary>Receipt-in (NFe Outras) detail for Imprimir page. PDF/XML under IN{ReceiptNo}/.</summary>
public class ReceiptInNfeDetailForDanfeDto
{
    public int ReceiptNo { get; set; }
    public string? SupplierName { get; set; }
    public string? NfeKey { get; set; }
    public string? NfeProtocol { get; set; }
    public string? NfeCancelProtocol { get; set; }
    public string? NfeReceipt { get; set; }
    public int? InternalReceipt { get; set; }
    /// <summary>Relative path e.g. "IN123/DFE....pdf" for /NFE_FILES/.</summary>
    public string? PdfRelativePath { get; set; }
    /// <summary>Relative path e.g. "IN123/DFE....xml" for /NFE_FILES/.</summary>
    public string? XmlRelativePath { get; set; }
}
