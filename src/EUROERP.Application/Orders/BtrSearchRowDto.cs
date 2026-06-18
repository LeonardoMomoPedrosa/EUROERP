namespace EUROERP.Application.Orders;

/// <summary>Legacy SalesService.searchBtr parity row for SAT/payment consumption.</summary>
public sealed class BtrSearchRowDto
{
    public decimal Amount { get; set; }
    public int Terms { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Psm { get; set; } = string.Empty;
    public string Cfe { get; set; } = "01";
}

