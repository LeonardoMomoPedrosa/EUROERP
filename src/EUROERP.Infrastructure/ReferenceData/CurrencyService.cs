using System.Data;
using Dapper;
using EUROERP.Application.ReferenceData;

namespace EUROERP.Infrastructure.ReferenceData;

/// <summary>Eurobus CurrencyController — list/update CURRENCY_CONVERSION rates only.</summary>
public class CurrencyService : ICurrencyService
{
    private readonly IDbConnection _connection;

    public CurrencyService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<CurrencyConversionDto>> GetConversionsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT cv.SOURCE_CURRENCY_ID AS SourceCurrencyId,
    cu.SYMBOL AS SourceSymbol,
    cu.NAME AS SourceName,
    cu2.SYMBOL AS TargetSymbol,
    cu2.NAME AS TargetName,
    cv.TARGET_CURRENCY_ID AS TargetCurrencyId,
    cv.CONVERSION AS Conversion
FROM CURRENCY_CONVERSION cv
JOIN CURRENCY cu ON cv.SOURCE_CURRENCY_ID = cu.PKId
JOIN CURRENCY cu2 ON cv.TARGET_CURRENCY_ID = cu2.PKId
ORDER BY cu2.NAME, cu.NAME";
        var list = await _connection.QueryAsync<CurrencyConversionDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task UpdateConversionAsync(byte sourceCurrencyId, byte targetCurrencyId, decimal conversion, CancellationToken cancellationToken = default)
    {
        if (conversion < 0 || conversion >= 10)
            throw new InvalidOperationException("Conversão deve estar entre 0 e 9,999 (decimal 4,3).");

        const string sql = @"
UPDATE CURRENCY_CONVERSION
SET CONVERSION = @Conversion
WHERE SOURCE_CURRENCY_ID = @SourceCurrencyId AND TARGET_CURRENCY_ID = @TargetCurrencyId";

        var rows = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Conversion = Math.Round(conversion, 3),
                SourceCurrencyId = sourceCurrencyId,
                TargetCurrencyId = targetCurrencyId
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (rows == 0)
            throw new InvalidOperationException("Conversão de moeda não encontrada.");
    }
}
