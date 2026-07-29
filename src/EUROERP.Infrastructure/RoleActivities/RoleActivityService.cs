using System.Data;
using Dapper;
using EUROERP.Application.Activities;
using EUROERP.Application.RoleActivities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.RoleActivities;

public sealed class RoleActivityService : IRoleActivityService
{
    private readonly IDbConnection _connection;
    private readonly string _applicationName;
    private readonly ILogger<RoleActivityService> _logger;

    public RoleActivityService(IDbConnection connection, IConfiguration configuration, ILogger<RoleActivityService> logger)
    {
        _connection = connection;
        _applicationName = configuration["Authentication:ApplicationName"] ?? "LionSystem";
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoleWithActivitiesDto>> GetRolesWithActivitiesAsync(CancellationToken cancellationToken = default)
    {
        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return Array.Empty<RoleWithActivitiesDto>();

        const string sqlRoles = @"
            SELECT RoleId, RoleName
            FROM aspnet_Roles
            WHERE ApplicationId = @ApplicationId
            ORDER BY RoleName";
        var roles = (await _connection.QueryAsync<RoleRow>(
            new CommandDefinition(sqlRoles, new { ApplicationId = appId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        if (roles.Count == 0)
            return Array.Empty<RoleWithActivitiesDto>();

        const string sqlAssignments = @"
            SELECT ar.ROLE_ID AS RoleId, CAST(ar.ACTV_ID AS int) AS ActvId
            FROM ACTIVITY_ROLE ar
            INNER JOIN aspnet_Roles r ON r.RoleId = ar.ROLE_ID AND r.ApplicationId = @ApplicationId";
        var assignments = (await _connection.QueryAsync<RoleActivityRow>(
            new CommandDefinition(sqlAssignments, new { ApplicationId = appId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        var actvIdsByRole = assignments.GroupBy(x => x.RoleId).ToDictionary(g => g.Key, g => g.Select(x => x.ActvId).ToList());

        return roles.Select(r => new RoleWithActivitiesDto
        {
            RoleId = r.RoleId,
            RoleName = r.RoleName,
            ActvIds = actvIdsByRole.TryGetValue(r.RoleId, out var ids) ? ids : new List<int>()
        }).ToList();
    }

    public async Task SetRoleActivitiesAsync(Guid roleId, IReadOnlyList<int> actvIds, CancellationToken cancellationToken = default)
    {
        await _connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM ACTIVITY_ROLE WHERE ROLE_ID = @RoleId",
            new { RoleId = roleId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        foreach (var actvId in actvIds.Distinct())
        {
            await _connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO ACTIVITY_ROLE (ACTV_ID, ROLE_ID) VALUES (@ActvId, @RoleId)",
                new { ActvId = actvId, RoleId = roleId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        _logger.LogInformation("Atividades do papel RoleId {RoleId} atualizadas: {Count} atividade(s).", roleId, actvIds.Count);
    }

    public async Task<IReadOnlyList<ActivityDto>> GetActivitiesForRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CAST(a.PKId AS int) AS ActvId, a.CODE AS Code, a.DESCRIPTION AS Description
            FROM SEC_ACTIVITY a
            INNER JOIN ACTIVITY_ROLE ar ON ar.ACTV_ID = a.PKId
            WHERE ar.ROLE_ID = @RoleId
            ORDER BY a.CODE";
        var rows = await _connection.QueryAsync<ActivityDto>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ActivityDto>> GetAvailableActivitiesForRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CAST(a.PKId AS int) AS ActvId, a.CODE AS Code, a.DESCRIPTION AS Description
            FROM SEC_ACTIVITY a
            WHERE NOT EXISTS (
                SELECT 1 FROM ACTIVITY_ROLE ar
                WHERE ar.ACTV_ID = a.PKId AND ar.ROLE_ID = @RoleId)
            ORDER BY a.CODE";
        var rows = await _connection.QueryAsync<ActivityDto>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task AddActivityToRoleAsync(Guid roleId, int actvId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM ACTIVITY_ROLE WHERE ACTV_ID = @ActvId AND ROLE_ID = @RoleId)
                INSERT INTO ACTIVITY_ROLE (ACTV_ID, ROLE_ID) VALUES (@ActvId, @RoleId)";
        await _connection.ExecuteAsync(new CommandDefinition(
            sql, new { ActvId = actvId, RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task RemoveActivityFromRoleAsync(Guid roleId, int actvId, CancellationToken cancellationToken = default)
    {
        await _connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM ACTIVITY_ROLE WHERE ACTV_ID = @ActvId AND ROLE_ID = @RoleId",
            new { ActvId = actvId, RoleId = roleId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<Guid?> GetApplicationIdAsync(CancellationToken ct)
    {
        const string sql = "SELECT ApplicationId FROM aspnet_Applications WHERE LoweredApplicationName = LOWER(@ApplicationName)";
        return await _connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { ApplicationName = _applicationName }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private sealed class RoleRow
    {
        public Guid RoleId { get; init; }
        public string RoleName { get; init; } = string.Empty;
    }

    private sealed class RoleActivityRow
    {
        public Guid RoleId { get; init; }
        public int ActvId { get; init; }
    }
}
