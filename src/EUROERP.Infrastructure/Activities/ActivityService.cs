using System.Data;
using Dapper;
using EUROERP.Application.Activities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.Activities;

public sealed class ActivityService : IActivityService
{
    private const int CodeMaxLength = 8;
    private const int DescriptionMaxLength = 30;

    /// <summary>SQL Server error numbers for foreign key violations.</summary>
    private const int ForeignKeyConflict = 547;

    private readonly IDbConnection _connection;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(IDbConnection connection, ILogger<ActivityService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ActivityDto>> ListActivitiesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CAST(PKId AS int) AS ActvId, CODE AS Code, DESCRIPTION AS Description
            FROM SEC_ACTIVITY
            ORDER BY CODE";
        var rows = await _connection.QueryAsync<ActivityDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<ActivityOperationResult> CreateAsync(string code, string description, CancellationToken cancellationToken = default)
    {
        var validation = Validate(ref code, ref description);
        if (validation != null)
            return validation;

        var exists = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1 FROM SEC_ACTIVITY WHERE CODE = @Code",
            new { Code = code },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (exists != 0)
            return ActivityOperationResult.Fail("Já existe uma atividade com este código.");

        await _connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO SEC_ACTIVITY (CODE, DESCRIPTION) VALUES (@Code, @Description)",
            new { Code = code, Description = description },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("Atividade criada: {Code}.", code);
        return ActivityOperationResult.Ok("Atividade criada.");
    }

    public async Task<ActivityOperationResult> UpdateAsync(int actvId, string code, string description, CancellationToken cancellationToken = default)
    {
        var validation = Validate(ref code, ref description);
        if (validation != null)
            return validation;

        var duplicate = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1 FROM SEC_ACTIVITY WHERE CODE = @Code AND PKId <> @ActvId",
            new { Code = code, ActvId = actvId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (duplicate != 0)
            return ActivityOperationResult.Fail("Já existe outra atividade com este código.");

        var affected = await _connection.ExecuteAsync(new CommandDefinition(
            "UPDATE SEC_ACTIVITY SET CODE = @Code, DESCRIPTION = @Description WHERE PKId = @ActvId",
            new { Code = code, Description = description, ActvId = actvId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (affected == 0)
            return ActivityOperationResult.Fail("Atividade não encontrada.");

        return ActivityOperationResult.Ok("Atividade atualizada.");
    }

    public async Task<ActivityOperationResult> DeleteAsync(int actvId, CancellationToken cancellationToken = default)
    {
        var inUse = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1 FROM ACTIVITY_ROLE WHERE ACTV_ID = @ActvId",
            new { ActvId = actvId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (inUse != 0)
            return ActivityOperationResult.Fail("Atividade está associada a um ou mais papéis. Remova as associações antes de excluir.");

        try
        {
            var affected = await _connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM SEC_ACTIVITY WHERE PKId = @ActvId",
                new { ActvId = actvId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (affected == 0)
                return ActivityOperationResult.Fail("Atividade não encontrada.");
        }
        catch (SqlException ex) when (ex.Number == ForeignKeyConflict)
        {
            _logger.LogWarning(ex, "Falha ao excluir atividade {ActvId}: referenciada por outra tabela.", actvId);
            return ActivityOperationResult.Fail("Atividade está em uso e não pode ser excluída.");
        }

        _logger.LogInformation("Atividade excluída: {ActvId}.", actvId);
        return ActivityOperationResult.Ok("Atividade excluída.");
    }

    private static ActivityOperationResult? Validate(ref string code, ref string description)
    {
        code = (code ?? string.Empty).Trim();
        description = (description ?? string.Empty).Trim();

        if (code.Length == 0)
            return ActivityOperationResult.Fail("Código é obrigatório.");
        if (code.Length > CodeMaxLength)
            return ActivityOperationResult.Fail($"Código deve ter no máximo {CodeMaxLength} caracteres.");
        if (description.Length == 0)
            return ActivityOperationResult.Fail("Descrição é obrigatória.");
        if (description.Length > DescriptionMaxLength)
            return ActivityOperationResult.Fail($"Descrição deve ter no máximo {DescriptionMaxLength} caracteres.");
        return null;
    }
}
