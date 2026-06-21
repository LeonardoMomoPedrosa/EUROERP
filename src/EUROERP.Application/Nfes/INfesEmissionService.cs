namespace EUROERP.Application.Nfes;

public interface INfesEmissionService
{
    Task<NfesOrderPreviewDto?> GetOrderPreviewAsync(int orderId, CancellationToken cancellationToken = default);
    Task<int> GetNextRpsNumberAsync(CancellationToken cancellationToken = default);
    Task<EmitNfesResult> EmitAsync(EmitNfesRequest request, CancellationToken cancellationToken = default);
    /// <summary>Fetches NFS-e XML (webservice or local file) and generates DANFSe PDF bytes.</summary>
    Task<NfesPrintPdfResult> GetDanfsePdfAsync(int orderId, CancellationToken cancellationToken = default);
}

public class NfesPrintPdfResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public byte[]? PdfBytes { get; set; }
    public string? FileName { get; set; }
}

public class NfesOrderPreviewDto
{
    public int OrderId { get; set; }
    public string Status { get; set; } = "";
    public string ClientName { get; set; } = "";
    public int ClientId { get; set; }
    public decimal ServiceTotal { get; set; }
    public string? NfesNo { get; set; }
    public string? RpsNo { get; set; }
    public string? NfesCheckCode { get; set; }
    public string? NfesChaveAcesso { get; set; }
    public string? PrintUrl { get; set; }
    public bool CanEmit { get; set; }
    public string? BlockReason { get; set; }
    public string? Provider { get; set; }
}

public class EmitNfesRequest
{
    public int OrderId { get; set; }
    /// <summary>RPS number; prefix R for reprint (legacy).</summary>
    public string RpsNumber { get; set; } = "";
    public byte MessageId { get; set; }
}

public class EmitNfesResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? NfesNo { get; set; }
    public string? RpsNo { get; set; }
    public string? CheckCode { get; set; }
    public string? NfesChaveAcesso { get; set; }
    public string? PrintUrl { get; set; }
    public string? XmlResponsePath { get; set; }
}
