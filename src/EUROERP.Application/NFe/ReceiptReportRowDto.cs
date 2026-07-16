namespace EUROERP.Application.NFe;

/// <summary>One row for the NFe receipt report (all receipts in a date range).</summary>
public class ReceiptReportRowDto
{
    public int ReceiptNo { get; set; }
    public string Cfop { get; set; } = "";
    public int OrderId { get; set; }
    public string RecDate { get; set; } = "";
    public string RecTime { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal NfAmount { get; set; }
    public string? CancelDate { get; set; }
    public string? CancelUser { get; set; }
    public string? CancelMemo { get; set; }
    public string? SocialName { get; set; }
    public string? OtherName { get; set; }
    public string? NfeKey { get; set; }
    public string? CnpjPf { get; set; }
    public string? OnfeKey { get; set; }
    public string Inout { get; set; } = "";
}
