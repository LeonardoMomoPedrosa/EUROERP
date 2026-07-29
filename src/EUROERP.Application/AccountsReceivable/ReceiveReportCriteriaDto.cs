namespace EUROERP.Application.AccountsReceivable;

/// <summary>Criteria for AR receive report (Relatório de Baixas).</summary>
public class ReceiveReportCriteriaDto
{
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
    public byte PaymentMethodId { get; set; }
}
