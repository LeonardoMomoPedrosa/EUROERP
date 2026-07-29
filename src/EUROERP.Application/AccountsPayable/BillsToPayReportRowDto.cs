namespace EUROERP.Application.AccountsPayable;

/// <summary>One row in the AP report (one bill term).</summary>
public class BillsToPayReportRowDto
{
    public int PkId { get; set; }
    public int SupplierId { get; set; }
    public string SysCreationDate { get; set; } = string.Empty;
    public string PymMethod { get; set; } = string.Empty;
    public byte PaymentMethodId { get; set; }
    public byte Terms { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string BillType { get; set; } = string.Empty;
    public int? StockInId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public byte CurrencyId { get; set; }
    public decimal Conversion { get; set; }
    public string SocialName { get; set; } = string.Empty;
    public byte TermNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public string DueDateOrder { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public decimal Paid { get; set; }
    public decimal ConvertedPaid { get; set; }
    public string OrderDate { get; set; } = string.Empty;
    public string? Bank { get; set; }
    public int Sgid { get; set; }
    public string? Hidden { get; set; }

    public decimal Balance => ConvertedAmount - ConvertedPaid;
}
