namespace EUROERP.Application.AccountsReceivable;

/// <summary>Result of AR receive report (Relatório de Baixas).</summary>
public class ReceiveReportResultDto
{
    public IReadOnlyList<ReceiveReportRowDto> Rows { get; init; } = new List<ReceiveReportRowDto>();
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
    public decimal TotalOriginal => Rows.Sum(r => r.OriginalAmount);
    public decimal TotalBaixa => Rows.Sum(r => r.Amount);
}
