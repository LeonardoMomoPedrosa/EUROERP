namespace EUROERP.Application.NFe;

/// <summary>One row for the NFe schedule (Emissão em Lote) grid.</summary>
public class ScheduleRowDto
{
    public int OrderId { get; set; }
    public string Date { get; set; } = "";
    public string Hour { get; set; } = "";
    public int Receipt { get; set; }
    public string FantasyName { get; set; } = "";
    public string SalesAgent { get; set; } = "";
    public string Status { get; set; } = "";
    public string? SiteOrderId { get; set; }
    public string SchStatus { get; set; } = "";
    public string SchErrorCode { get; set; } = "";
    public decimal SchDiscount { get; set; }
    public string SchDesc { get; set; } = "";
    public string SendText { get; set; } = "";
    public string? XmlFileName { get; set; }
    public string? PdfFileName { get; set; }
}
