namespace EUROERP.Application.Nfes;

public interface INfesEmissionService
{
    Task<NfesOrderPreviewDto?> GetOrderPreviewAsync(int orderId, CancellationToken cancellationToken = default);
    Task<int> GetNextRpsNumberAsync(CancellationToken cancellationToken = default);
    Task<EmitNfesResult> EmitAsync(EmitNfesRequest request, CancellationToken cancellationToken = default);
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
    public string? XmlResponsePath { get; set; }
}
