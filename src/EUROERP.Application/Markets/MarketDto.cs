namespace EUROERP.Application.Markets;

/// <summary>Market row from MARKET (Epic 17).</summary>
public sealed class MarketDto
{
    public byte Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
