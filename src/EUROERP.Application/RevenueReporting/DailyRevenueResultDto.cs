namespace EUROERP.Application.RevenueReporting;

/// <summary>Result of daily revenue report (faturamento diário).</summary>
public class DailyRevenueResultDto
{
    public IReadOnlyList<DailyRevenueRowDto> Rows { get; set; } = new List<DailyRevenueRowDto>();
    public IReadOnlyList<PaymentMethodTotalDto> TotalsByPaymentMethod { get; set; } = new List<PaymentMethodTotalDto>();
    public decimal TotalLoja { get; set; }
    public decimal TotalSite { get; set; }
    public decimal TotalMeli { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<SpecialClientTotalDto> SpecialClientTotals { get; set; } = new List<SpecialClientTotalDto>();
}
