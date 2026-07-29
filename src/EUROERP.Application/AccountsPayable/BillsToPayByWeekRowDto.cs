namespace EUROERP.Application.AccountsPayable;

/// <summary>One term row for the AP daily (by week) report. Aggregation by day is done in the UI.</summary>
public class BillsToPayByWeekRowDto
{
    public int FinanceBillId { get; set; }
    public byte TermNo { get; set; }
    public int Week { get; set; }
    public int WeekDay { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public string DueDateOrder { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal ConvertedAmount { get; set; }
    public decimal ConvertedPaid { get; set; }
    public byte CurrencyId { get; set; }

    public decimal Balance => Amount - Paid;
}
