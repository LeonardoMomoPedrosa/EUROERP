namespace EUROERP.Application.RevenueReporting;

public class MonthlySupplierRevenueResultDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public byte Month { get; set; }
    public int Year { get; set; }
    public IReadOnlyList<MonthlySupplierRevenueDayRowDto> Days { get; init; } = new List<MonthlySupplierRevenueDayRowDto>();
    public decimal TotalAmount { get; set; }
    public decimal TotalBaixa { get; set; }
    public decimal TotalUso { get; set; }
    public decimal TotalFp { get; set; }
}
