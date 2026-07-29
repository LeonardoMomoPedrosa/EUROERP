namespace EUROERP.Application.RevenueReporting;

/// <summary>Criteria for daily revenue (faturamento diário) report.</summary>
public class DailyRevenueCriteriaDto
{
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
    /// <summary>0 = all payment methods.</summary>
    public byte PaymentMethodId { get; set; }
    /// <summary>"Selecione" = all sales agents.</summary>
    public string SalesAgentName { get; set; } = "Selecione";
    /// <summary>"Selecione" = all vendedores (of saler).</summary>
    public string OfSalerName { get; set; } = "Selecione";
    /// <summary>When &gt; 0, filter orders that contain products from this supplier (Eurobus ?supId=).</summary>
    public int SupplierId { get; set; }
}
