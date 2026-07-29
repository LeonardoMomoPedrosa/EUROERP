using System.Data;
using Dapper;
using EUROERP.Application.Widgets;

namespace EUROERP.Infrastructure.Widgets;

public sealed class UserShortcutService : IUserShortcutService
{
    private readonly IDbConnection _connection;

    public UserShortcutService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<UserShortcutDto>> GetShortcutsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sql = "SELECT Route, SortOrder FROM USER_SHORTCUT WHERE UserId = @UserId ORDER BY SortOrder";
        try
        {
            var list = (await _connection.QueryAsync<UserShortcutDto>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
            return list;
        }
        catch
        {
            // USER_SHORTCUT may not exist yet (see docs/sql/user_shortcut_create.sql).
            return Array.Empty<UserShortcutDto>();
        }
    }

    public async Task SetShortcutsAsync(Guid userId, IReadOnlyList<string> routes, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        await _connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM USER_SHORTCUT WHERE UserId = @UserId", new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (routes == null || routes.Count == 0)
            return;

        const string insert = "INSERT INTO USER_SHORTCUT (UserId, Route, SortOrder) VALUES (@UserId, @Route, @SortOrder)";
        var distinctRoutes = routes.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct().ToList();
        for (var i = 0; i < distinctRoutes.Count; i++)
        {
            await _connection.ExecuteAsync(
                new CommandDefinition(insert, new { UserId = userId, Route = distinctRoutes[i], SortOrder = i }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}
