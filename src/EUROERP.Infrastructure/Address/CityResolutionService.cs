using System.Data;
using Dapper;
using EUROERP.Application.Address;

namespace EUROERP.Infrastructure.Address;

public class CityResolutionService : ICityResolutionService
{
    private readonly IDbConnection _connection;
    private readonly IViaCepService _viaCepService;

    public CityResolutionService(IDbConnection connection, IViaCepService viaCepService)
    {
        _connection = connection;
        _viaCepService = viaCepService;
    }

    public async Task<short> ResolveOrCreateCityAsync(byte stateId, string cityName, string? ibgeCode, string? cep = null, CancellationToken cancellationToken = default)
    {
        var name = (cityName ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return 0;

        var nameTruncated = name.Length > 30 ? name[..30] : name;

        const string sqlFind = @"
            SELECT c.PKId FROM CITY c
            WHERE c.STATE_ID = @StateId
            AND c.NAME COLLATE Latin1_General_CI_AI = @CityName COLLATE Latin1_General_CI_AI";

        var existing = await _connection.QueryFirstOrDefaultAsync<short>(
            new CommandDefinition(sqlFind, new { StateId = stateId, CityName = nameTruncated }, cancellationToken: cancellationToken));

        var cMun = string.IsNullOrWhiteSpace(ibgeCode) ? null : new string(ibgeCode.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(cMun) && cMun.Length > 7)
            cMun = cMun[..7];

        if (existing > 0)
        {
            if (!string.IsNullOrEmpty(cMun))
            {
                const string sqlUpdateExisting = @"
                    UPDATE CITY
                    SET C_MUN = @CMun
                    WHERE PKId = @CityId
                    AND ISNULL(CONVERT(varchar(20), C_MUN), '') <> @CMun";
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlUpdateExisting, new { CityId = existing, CMun = cMun }, cancellationToken: cancellationToken));
            }

            return existing;
        }

        // Cidade não existe: obter C_MUN. Se não veio ibgeCode, tentar ViaCEP quando cep tiver 8 dígitos (evita chamada externa quando cidade já existe).
        if (string.IsNullOrEmpty(cMun) && !string.IsNullOrEmpty(cep))
        {
            var cepDigits = new string(cep.Where(char.IsDigit).ToArray());
            if (cepDigits.Length == 8)
            {
                var viaCep = await _viaCepService.GetByCepAsync(cepDigits, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(viaCep?.Ibge))
                    cMun = new string(viaCep.Ibge.Where(char.IsDigit).ToArray());
            }
        }
        if (string.IsNullOrEmpty(cMun))
            cMun = "0";
        if (cMun.Length > 7)
            cMun = cMun[..7];

        const string sqlInsert = @"
            INSERT INTO CITY (NAME, STATE_ID, C_MUN)
            VALUES (@Name, @StateId, @CMun);
            SELECT CAST(SCOPE_IDENTITY() AS smallint);";

        var newId = await _connection.ExecuteScalarAsync<short>(
            new CommandDefinition(sqlInsert, new { Name = nameTruncated, StateId = stateId, CMun = cMun }, cancellationToken: cancellationToken));

        return newId;
    }
}
