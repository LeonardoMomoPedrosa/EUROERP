namespace EUROERP.Application.NFe;

public class CancelNfeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? CancelProtocol { get; set; }
    public string? SefazCStat { get; set; }
    public string? SefazXMotivo { get; set; }
    public string? SefazXEvento { get; set; }
}
