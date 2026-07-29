using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Dapper;
using EUROERP.Application.Master;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.Master;

public sealed class MasterSqlService : IMasterSqlService
{
    private const int CommandTimeoutSeconds = 120;
    private const int MaxRows = 1000;

    /// <summary>Matches a statement that starts with SELECT, or a CTE (WITH ...) which always ends in a query.</summary>
    private static readonly Regex SelectPrefix = new(@"^\s*(select|with)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IDbConnection _connection;
    private readonly ILogger<MasterSqlService> _logger;

    public MasterSqlService(IDbConnection connection, ILogger<MasterSqlService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public bool IsSelect(string sql) => !string.IsNullOrWhiteSpace(sql) && SelectPrefix.IsMatch(StripLeadingComments(sql));

    public async Task<MasterSqlResult> ExecuteAsync(string sql, string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return MasterSqlResult.Fail("Digite um comando SQL.");

        var isQuery = IsSelect(sql);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogWarning("Master SQL executado por {UserName} ({Kind}): {Sql}", userName, isQuery ? "consulta" : "comando", sql);

        try
        {
            if (isQuery)
            {
                var rows = (await _connection.QueryAsync(new CommandDefinition(
                    sql, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
                stopwatch.Stop();

                var truncated = rows.Count > MaxRows;
                var materialized = rows
                    .Take(MaxRows)
                    .Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>((IDictionary<string, object?>)r))
                    .ToList();

                var columns = materialized.Count > 0
                    ? materialized[0].Keys.ToList()
                    : new List<string>();

                return new MasterSqlResult
                {
                    Success = true,
                    IsQuery = true,
                    Columns = columns,
                    Rows = materialized,
                    RowsAffected = materialized.Count,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Message = truncated
                        ? $"{materialized.Count} linha(s) exibida(s) (resultado truncado em {MaxRows})."
                        : $"{materialized.Count} linha(s)."
                };
            }

            var affected = await _connection.ExecuteAsync(new CommandDefinition(
                sql, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
            stopwatch.Stop();

            return new MasterSqlResult
            {
                Success = true,
                IsQuery = false,
                RowsAffected = affected,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Message = $"Comando executado. {affected} linha(s) afetada(s)."
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Master SQL falhou para {UserName}.", userName);
            return new MasterSqlResult
            {
                Success = false,
                IsQuery = isQuery,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Message = ex.Message
            };
        }
    }

    private static string StripLeadingComments(string sql)
    {
        var text = sql;
        while (true)
        {
            text = text.TrimStart();
            if (text.StartsWith("--", StringComparison.Ordinal))
            {
                var eol = text.IndexOf('\n');
                if (eol < 0) return string.Empty;
                text = text[(eol + 1)..];
                continue;
            }
            if (text.StartsWith("/*", StringComparison.Ordinal))
            {
                var end = text.IndexOf("*/", StringComparison.Ordinal);
                if (end < 0) return string.Empty;
                text = text[(end + 2)..];
                continue;
            }
            return text;
        }
    }
}
