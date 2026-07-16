namespace EUROERP.Application.NFe;

/// <summary>Order and client info for NFe Individual page (Venda).</summary>
public class OrderInfoForNfeDto
{
    public string ClientName { get; set; } = "";
    public string? Address { get; set; }
    public string Status { get; set; } = "";
    public decimal OrderTotal { get; set; }
    /// <summary>Valor do frete do pedido (SHIPMENT_COST). Incluído na NFe no primeiro item e no total.</summary>
    public decimal ShipmentCost { get; set; }
    /// <summary>Desconto do pedido (ORDER.DISCOUNT), percentual 0–99. Ajustável antes da emissão.</summary>
    public decimal Discount { get; set; }
    /// <summary>Crédito do pedido (ORDER.CREDIT). Usado no cálculo do desconto para NFe.</summary>
    public decimal Credit { get; set; }
    /// <summary>Outras despesas (ORDER.OTHER_EXPENSES). Incluído no total da NFe como vOutro.</summary>
    public decimal OtherExpenses { get; set; }
    public int ProductCount { get; set; }
    public string? NfeReceipt { get; set; }
    public string? NfeProtocolResult { get; set; }
    public string? NfeProtocol { get; set; }
    /// <summary>Chave de acesso da NFe (44 dígitos), quando o pedido já possui NFe emitida.</summary>
    public string? NfeKey { get; set; }
    /// <summary>Caminho relativo do PDF da NFe atual (ex.: 123/DFE44chars.pdf), para exibir link quando já emitida.</summary>
    public string? PdfRelativePath { get; set; }
    /// <summary>Caminho relativo do XML da NFe atual, para exibir link quando já emitida.</summary>
    public string? XmlRelativePath { get; set; }
    public int? Receipt { get; set; }
    /// <summary>CFOP codes/descriptions for this order (for display).</summary>
    public IReadOnlyList<CfopItemDto> CfopList { get; set; } = Array.Empty<CfopItemDto>();
}
