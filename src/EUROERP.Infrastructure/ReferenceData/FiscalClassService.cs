using System.Data;
using Dapper;
using EUROERP.Application.ReferenceData;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.ReferenceData;

public class FiscalClassService : IFiscalClassService
{
    private const int SqlErrorForeignKeyConstraint = 547;
    private const int SqlErrorUniqueKeyViolation = 2627;
    private readonly IDbConnection _connection;

    public FiscalClassService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<FiscalClassDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT PKId AS Id, VALUE AS Value, IPI AS Ipi, ISNULL(ICMSST, 0) AS Icmsst, NAME AS Name
            FROM FISCAL_CLASS
            ORDER BY PKId";
        var list = await _connection.QueryAsync<FiscalClassDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task CreateAsync(int id, string value, decimal? ipi, bool icmsst, string name, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;
        var valueTrim = (value ?? "").Trim();
        if (valueTrim.Length > 20) valueTrim = valueTrim[..20];
        var nameTrim = (name ?? "").Trim();
        if (nameTrim.Length > 50) nameTrim = nameTrim[..50];

        const string sql = @"
            INSERT INTO [FISCAL_CLASS] ([PKId], [VALUE], [IPI], [ICMSST], [NAME], [SYS_CREATION_DATE], [APPLICATION_ID], [USER_ID])
            VALUES (@Id, @Value, @Ipi, @Icmsst, @Name, GETDATE(), @ApplicationId, @UserId)";

        try
        {
            await _connection.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id, Value = valueTrim, Ipi = ipi ?? 0, Icmsst = icmsst, Name = nameTrim, ApplicationId = appId, UserId = uid }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == SqlErrorUniqueKeyViolation)
        {
            throw new InvalidOperationException($"Já existe uma classe fiscal com o Id {id}. Escolha outro Id.", ex);
        }
    }

    public async Task UpdateAsync(int id, string value, decimal? ipi, bool icmsst, string name, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;
        var valueTrim = (value ?? "").Trim();
        if (valueTrim.Length > 20) valueTrim = valueTrim[..20];
        var nameTrim = (name ?? "").Trim();
        if (nameTrim.Length > 50) nameTrim = nameTrim[..50];

        const string sql = @"
            UPDATE [FISCAL_CLASS]
            SET [VALUE] = @Value, [IPI] = @Ipi, [ICMSST] = @Icmsst, [NAME] = @Name, [SYS_UPDATE_DATE] = GETDATE(),
                [APPLICATION_ID] = @ApplicationId, [USER_ID] = @UserId
            WHERE PKId = @Id";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, Value = valueTrim, Ipi = ipi ?? 0, Icmsst = icmsst, Name = nameTrim, ApplicationId = appId, UserId = uid }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM [FISCAL_CLASS] WHERE [PKId] = @Id";
        try
        {
            await _connection.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == SqlErrorForeignKeyConstraint)
        {
            throw new InvalidOperationException("Erro ao excluir classe fiscal. Verifique se existem produtos associados.", ex);
        }
    }
}
