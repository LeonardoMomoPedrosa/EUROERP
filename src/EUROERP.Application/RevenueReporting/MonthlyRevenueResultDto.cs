namespace EUROERP.Application.RevenueReporting;

/// <summary>Result of monthly revenue report (faturamento mensal geral).</summary>
public class MonthlyRevenueResultDto
{
    public byte Month { get; set; }
    public int Year { get; set; }
    public IReadOnlyList<MonthlyRevenueDayRowDto> Days { get; set; } = new List<MonthlyRevenueDayRowDto>();
    public decimal TotalLoja { get; set; }
    public decimal TotalSite { get; set; }
    public decimal TotalMeli { get; set; }
    public decimal TotalDev { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalBaixa { get; set; }
    public decimal TotalUso { get; set; }
    public decimal TotalFp { get; set; }
    public decimal TotalMortM { get; set; }
    public decimal TotalMortD { get; set; }
}
