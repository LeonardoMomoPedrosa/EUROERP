namespace EUROERP.Application.NFe;

public class EmitNfeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? NfeKey { get; set; }
    public string? PdfRelativePath { get; set; }
    public string? XmlRelativePath { get; set; }
}
