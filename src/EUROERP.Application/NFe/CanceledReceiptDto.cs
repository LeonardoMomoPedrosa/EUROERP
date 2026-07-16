namespace EUROERP.Application.NFe;

public class CanceledReceiptDto
{
    public int ReceiptNo { get; set; }
    public DateTime CancelDate { get; set; }
    public string? Memo { get; set; }
}
