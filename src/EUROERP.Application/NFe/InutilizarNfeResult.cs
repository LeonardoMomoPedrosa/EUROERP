namespace EUROERP.Application.NFe;

public class InutilizarNfeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? InutProtocol { get; set; }
    public string? SefazCStat { get; set; }
    public string? SefazXMotivo { get; set; }
}
