namespace EUROERP.Application.AccountsReceivable;

/// <summary>One row in the receive report (Relatório de Baixas).</summary>
public class ReceiveReportRowDto
{
    public int OrderId { get; set; }
    public byte TermNo { get; set; }
    public byte Terms { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public string ReceiveDate { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal Amount { get; set; }
    public string? Memo { get; set; }
    public long ComId { get; set; }
    public int ReturnId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string ClientFantasyName { get; set; } = string.Empty;
}
