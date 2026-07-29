namespace EUROERP.Application.AccountsReceivable;

/// <summary>One row in the AR report (one BTR term).</summary>
public class BillsToReceiveReportRowDto
{
    public int OrderId { get; set; }
    public int BtrId { get; set; }
    public int Receipt { get; set; }
    public int NfesNo { get; set; }
    public byte TermNo { get; set; }
    public byte Terms { get; set; }
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public long ComId { get; set; }
    public string OrderDate { get; set; } = string.Empty;
    public string Ledge { get; set; } = "Y";
    public string DueDate { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public string? FantasyName { get; set; }
    public string SalesAgent { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentSubMethod { get; set; } = string.Empty;

    public decimal Balance => Amount - Paid;

    public string PaymentMethodDisplay =>
        string.IsNullOrWhiteSpace(PaymentSubMethod)
            ? PaymentMethod
            : $"{PaymentMethod} {PaymentSubMethod}".Trim();
}
