namespace EUROERP.Application.Orders;

public class SalesAgentDto
{
    public string UserName { get; set; } = string.Empty;
}

public class LastOrderDto
{
    public int OrderId { get; set; }
    public string FantasyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Mode { get; set; } = "S";
    public string ModeDescription { get; set; } = string.Empty;
}

/// <summary>Create OS (ORDER). Eurobus: MODE S = Venda, Q = Orçamento.</summary>
public class CreateOrderDto
{
    public int ClientId { get; set; }
    public string? SalesAgent { get; set; }
    public string OrderType { get; set; } = "P";
    /// <summary>S = Venda, Q = Orçamento (Eurobus ORDER.MODE).</summary>
    public string Mode { get; set; } = "S";
}
