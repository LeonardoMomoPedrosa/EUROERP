namespace EUROERP.Application.NFe;

public sealed class NfeCertificateUploadResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StoredPath { get; set; }
    public string? Subject { get; set; }
    public string? Issuer { get; set; }
    public string? Thumbprint { get; set; }
    public DateTime? NotBefore { get; set; }
    public DateTime? NotAfter { get; set; }
}

