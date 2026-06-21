using System.Data;
using Dapper;
using EUROERP.Application.Config;

namespace EUROERP.Infrastructure.Config;

public class SysControlService : ISysControlService
{
    public const int MaxValueLength = 50;

    private readonly IDbConnection _connection;

    public SysControlService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<string?> GetValueAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT VALUE FROM SYS_CONTROL WHERE CODE = @Code";
        return await _connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { Code = code }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default)
    {
        var codeList = codes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (codeList.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        const string sql = "SELECT CODE, VALUE FROM SYS_CONTROL WHERE CODE IN @Codes";
        var rows = await _connection.QueryAsync<SysControlRow>(
            new CommandDefinition(sql, new { Codes = codeList }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToDictionary(r => r.Code, r => r.Value ?? "", StringComparer.OrdinalIgnoreCase);
    }

    public Task SetValueAsync(string code, string value, CancellationToken cancellationToken = default) =>
        SaveValuesAsync(new Dictionary<string, string> { [code] = value }, cancellationToken);

    public async Task SaveValuesAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        foreach (var (code, value) in values)
        {
            if (code.Length > 50)
                throw new ArgumentException($"CODE '{code}' excede 50 caracteres.");
            if (value.Length > MaxValueLength)
                throw new ArgumentException($"Valor de '{code}' excede {MaxValueLength} caracteres.");
        }

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using var tx = _connection.BeginTransaction();
        try
        {
            foreach (var (code, value) in values)
            {
                const string upd = "UPDATE SYS_CONTROL SET VALUE = @Value WHERE CODE = @Code";
                var affected = await _connection.ExecuteAsync(
                    new CommandDefinition(upd, new { Code = code, Value = value }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
                if (affected == 0)
                {
                    const string ins = "INSERT INTO SYS_CONTROL (CODE, VALUE) VALUES (@Code, @Value)";
                    await _connection.ExecuteAsync(
                        new CommandDefinition(ins, new { Code = code, Value = value }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
                }
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private sealed class SysControlRow
    {
        public string Code { get; init; } = "";
        public string? Value { get; init; }
    }
}
