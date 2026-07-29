namespace EUROERP.Application.ReferenceData;

public interface ICurrencyService
{
    Task<IReadOnlyList<CurrencyConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default);
    Task UpdateConversionAsync(byte sourceCurrencyId, byte targetCurrencyId, decimal conversion, CancellationToken cancellationToken = default);
}
