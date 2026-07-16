namespace EUROERP.Application.NFe;

/// <summary>Eurobus RECEIPT_IN_DATA header (NFe Outras / Avulsa).</summary>
public class ReceiptInNfeDataDto
{
    public int ReceiptNo { get; set; }
    public byte Version { get; set; }
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public byte CfopId { get; set; }
    public string InOut { get; set; } = "I";
    public byte ShipmOrigin { get; set; } = 2;
    public decimal Shipment { get; set; }
    public string? NfeRef { get; set; }
    public string? Obs { get; set; }
    public string? Obs2 { get; set; }
    public string? Obs3 { get; set; }
    public decimal StAmount { get; set; }
    public decimal StBaseCalc { get; set; }
    public decimal BaseCalc { get; set; }
    public short IcmsPerc { get; set; }
    public decimal Conversion { get; set; } = 1;
    public int Volumes { get; set; }
    public int WeightGross { get; set; }
    public int WeightNet { get; set; }
    public string? Especie { get; set; }
    public decimal OtherAmount { get; set; }
    public string Csosn { get; set; } = "102";
    public string? NfeKey { get; set; }
    public string? NfeProtocol { get; set; }
}

public class SaveReceiptInNfeV1Request
{
    public int ReceiptNo { get; set; }
    public int SupplierId { get; set; }
    public byte CfopId { get; set; }
    public string InOut { get; set; } = "I";
    public byte ShipmOrigin { get; set; } = 2;
    public decimal Shipment { get; set; }
    public decimal StAmount { get; set; }
    public string? NfeRef { get; set; }
    public string? Obs { get; set; }
    public string UserId { get; set; } = "SYS";
    public string ApplicationId { get; set; } = "EUROERP";
}

public class SaveReceiptInNfeV2Request
{
    public int ReceiptNo { get; set; }
    public int SupplierId { get; set; }
    public byte CfopId { get; set; }
    public string InOut { get; set; } = "I";
    public byte ShipmOrigin { get; set; } = 2;
    public decimal Shipment { get; set; }
    public decimal BaseCalc { get; set; }
    public short IcmsPerc { get; set; }
    public decimal StBaseCalc { get; set; }
    public decimal StAmount { get; set; }
    public decimal OtherAmount { get; set; }
    public decimal Conversion { get; set; } = 1;
    public int Volumes { get; set; }
    public int WeightGross { get; set; }
    public int WeightNet { get; set; }
    public string? Especie { get; set; }
    public string Csosn { get; set; } = "102";
    public string? NfeRef { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public string? Obs3 { get; set; }
    public string UserId { get; set; } = "SYS";
    public string ApplicationId { get; set; } = "EUROERP";
}

public class ReceiptInDetailDto
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string FiscalClass { get; set; } = "";
    public decimal Ipi { get; set; }
    public decimal Icms { get; set; }
}

public class AddReceiptInDetailRequest
{
    public int ReceiptNo { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string FiscalClass { get; set; } = "";
    public decimal Ipi { get; set; }
    public decimal Icms { get; set; }
}

public class SaveReceiptInNfeResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int ReceiptNo { get; set; }
}
