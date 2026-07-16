namespace EUROERP.Application.NFe;

public class LastCcEventDto
{
    public int ReceiptNo { get; set; }
    public int OrderId { get; set; }
    public string NfeKey { get; set; } = "";
    public string? Protocol { get; set; }
    public string Reason { get; set; } = "";
    public DateTime SysCreationDate { get; set; }
    public string UserId { get; set; } = "";
    public string? Email { get; set; }
}
