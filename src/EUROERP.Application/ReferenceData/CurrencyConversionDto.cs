namespace EUROERP.Application.ReferenceData;

public class CurrencyConversionDto
{
    public byte SourceCurrencyId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceSymbol { get; set; } = string.Empty;
    public byte TargetCurrencyId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string TargetSymbol { get; set; } = string.Empty;
    public decimal Conversion { get; set; }
}
