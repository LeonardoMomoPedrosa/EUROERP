namespace EUROERP.Application.AccountsReceivable;

/// <summary>Search criteria for AR (Contas a Receber). Eurobus <c>BtrSearchCriteriaInfo</c>.</summary>
public class BillsToReceiveSearchCriteriaDto
{
    public DateTime? FirstDate { get; set; }
    public DateTime? LastDate { get; set; }
    public int ClientId { get; set; }
    public byte PaymentMethodId { get; set; }
    /// <summary>0 = all, P = paid only, U = unpaid only.</summary>
    public string Status { get; set; } = "0";
    /// <summary>Order's sales agent (UserName). "Selecione" = no filter.</summary>
    public string? SalesAgentName { get; set; }
    /// <summary>Vendedor linked to client via CLIENT_SALES_AGENTS_LINK (UserName). "Selecione" = no filter.</summary>
    public string? OfSalerName { get; set; }
    /// <summary>When set, search by order ID only (ignores date range).</summary>
    public int OrderId { get; set; }
    public bool Abc { get; set; }
    /// <summary>Order status filter: E = Sent (Eurobus default).</summary>
    public string OrderStatus { get; set; } = "E";
}
