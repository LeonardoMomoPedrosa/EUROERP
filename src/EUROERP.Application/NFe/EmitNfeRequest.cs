namespace EUROERP.Application.NFe;

public class EmitNfeRequest
{
    public int OrderId { get; set; }
    /// <summary>NFe number (or "R" + number for reprint).</summary>
    public string NfeNumber { get; set; } = "";
    public byte FreightEmitenteDestinatario { get; set; } // 0 = Emitente, 1 = Destinatário
    public byte CfopId { get; set; } // 0 = auto from order
    /// <summary>Desconto do pedido (ORDER.DISCOUNT), percentual 0–99. Quando informado, atualizado antes da emissão (emissão individual).</summary>
    public decimal? Discount { get; set; }
    public short? MessageId { get; set; }
    public int? TransportSupplierId { get; set; }
    public string? PackageSpecies { get; set; }
    public string? PackageQuantity { get; set; }
    public string? WeightGross { get; set; }
    public string? WeightNet { get; set; }
    /// <summary>Informações complementares (infCpl) — opcional; enviado à SEFAZ e impresso no DANFE.</summary>
    public string? InformacoesComplementares { get; set; }
}
