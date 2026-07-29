using System.Data;
using Dapper;
using EUROERP.Application.UserRoles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.UserRoles;

public sealed class UserRolesService : IUserRolesService
{
    private readonly IDbConnection _connection;
    private readonly string _applicationName;
    private readonly ILogger<UserRolesService> _logger;

    public UserRolesService(IDbConnection connection, IConfiguration configuration, ILogger<UserRolesService> logger)
    {
        _connection = connection;
        _applicationName = configuration["Authentication:ApplicationName"] ?? "LionSystem";
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserWithRolesDto>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return Array.Empty<UserWithRolesDto>();

        const string sqlUsers = @"
            SELECT u.UserId, u.UserName
            FROM aspnet_Users u
            INNER JOIN aspnet_Membership m ON m.UserId = u.UserId
            WHERE u.ApplicationId = @ApplicationId
            ORDER BY u.UserName";
        var users = (await _connection.QueryAsync<UserRow>(
            new CommandDefinition(sqlUsers, new { ApplicationId = appId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        if (users.Count == 0)
            return Array.Empty<UserWithRolesDto>();

        const string sqlRoles = @"
            SELECT ur.UserId, ur.RoleId
            FROM aspnet_UsersInRoles ur
            INNER JOIN aspnet_Roles r ON r.RoleId = ur.RoleId AND r.ApplicationId = @ApplicationId";
        var assignments = (await _connection.QueryAsync<UserRoleRow>(
            new CommandDefinition(sqlRoles, new { ApplicationId = appId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        var roleIdsByUser = assignments.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList());

        return users.Select(u => new UserWithRolesDto
        {
            UserId = u.UserId,
            UserName = u.UserName,
            RoleIds = roleIdsByUser.TryGetValue(u.UserId, out var ids) ? ids : new List<Guid>()
        }).ToList();
    }

    public async Task SetUserRolesAsync(Guid userId, IReadOnlyList<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        await _connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM aspnet_UsersInRoles WHERE UserId = @UserId",
            new { UserId = userId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        foreach (var roleId in roleIds)
        {
            await _connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO aspnet_UsersInRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
                new { UserId = userId, RoleId = roleId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        _logger.LogInformation("Atribuições atualizadas para UserId {UserId}: {Count} papel(eis).", userId, roleIds.Count);
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT r.RoleName
            FROM aspnet_UsersInRoles ur
            INNER JOIN aspnet_Roles r ON r.RoleId = ur.RoleId
            WHERE ur.UserId = @UserId
            ORDER BY r.RoleName";
        var names = await _connection.QueryAsync<string>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return names.ToList();
    }

    private async Task<Guid?> GetApplicationIdAsync(CancellationToken ct)
    {
        const string sql = "SELECT ApplicationId FROM aspnet_Applications WHERE LoweredApplicationName = LOWER(@ApplicationName)";
        return await _connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { ApplicationName = _applicationName }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private sealed class UserRow
    {
        public Guid UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
    }

    private sealed class UserRoleRow
    {
        public Guid UserId { get; init; }
        public Guid RoleId { get; init; }
    }
}
