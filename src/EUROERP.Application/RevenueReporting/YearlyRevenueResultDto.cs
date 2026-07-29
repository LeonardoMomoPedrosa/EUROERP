namespace EUROERP.Application.RevenueReporting;

/// <summary>Result of yearly revenue report (faturamento anual).</summary>
public class YearlyRevenueResultDto
{
    public IReadOnlyList<YearlyRevenueMonthRowDto> Months { get; set; } = new List<YearlyRevenueMonthRowDto>();
    public int TotalEnv { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalDev { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalIntern { get; set; }
    public decimal TotalLoja { get; set; }
    public decimal TotalBaixa { get; set; }
    public decimal TotalUso { get; set; }
    public decimal TotalFp { get; set; }
    public decimal TotalMortM { get; set; }
    public decimal TotalMortD { get; set; }
}
