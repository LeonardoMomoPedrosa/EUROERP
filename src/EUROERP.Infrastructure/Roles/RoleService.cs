using System.Data;
using Dapper;
using EUROERP.Application.Roles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.Roles;

public sealed class RoleService : IRoleService
{
    private static readonly HashSet<string> ProtectedRoleNames = new(StringComparer.OrdinalIgnoreCase) { "Admin", "Master" };

    private readonly IDbConnection _connection;
    private readonly string _applicationName;
    private readonly ILogger<RoleService> _logger;

    public RoleService(IDbConnection connection, IConfiguration configuration, ILogger<RoleService> logger)
    {
        _connection = connection;
        _applicationName = configuration["Authentication:ApplicationName"] ?? "LionSystem";
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return Array.Empty<RoleDto>();

        const string sql = @"
            SELECT RoleId, RoleName, Description
            FROM aspnet_Roles
            WHERE ApplicationId = @ApplicationId
            ORDER BY RoleName";
        var rows = await _connection.QueryAsync<RoleDto>(
            new CommandDefinition(sql, new { ApplicationId = appId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<RoleOperationResult> CreateRoleAsync(string roleName, string? description, CancellationToken cancellationToken = default)
    {
        var trimmed = roleName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return RoleOperationResult.Fail("Nome do papel é obrigatório.");

        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return RoleOperationResult.Fail("Aplicação não configurada.");

        var lowered = trimmed.ToLowerInvariant();
        var exists = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1 FROM aspnet_Roles WHERE ApplicationId = @ApplicationId AND LoweredRoleName = @LoweredRoleName",
            new { ApplicationId = appId.Value, LoweredRoleName = lowered },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (exists != 0)
            return RoleOperationResult.Fail("Já existe um papel com este nome.");

        var roleId = Guid.NewGuid();
        await _connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO aspnet_Roles (ApplicationId, RoleId, RoleName, LoweredRoleName, Description) VALUES (@ApplicationId, @RoleId, @RoleName, @LoweredRoleName, @Description)",
            new { ApplicationId = appId.Value, RoleId = roleId, RoleName = trimmed, LoweredRoleName = lowered, Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim() },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("Papel criado: {RoleName}, RoleId: {RoleId}.", trimmed, roleId);
        return RoleOperationResult.Ok("Papel criado.");
    }

    public async Task<RoleOperationResult> UpdateRoleAsync(Guid roleId, string roleName, string? description, CancellationToken cancellationToken = default)
    {
        var trimmed = roleName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return RoleOperationResult.Fail("Nome do papel é obrigatório.");

        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return RoleOperationResult.Fail("Aplicação não configurada.");

        var lowered = trimmed.ToLowerInvariant();
        var exists = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1 FROM aspnet_Roles WHERE ApplicationId = @ApplicationId AND RoleId = @RoleId",
            new { ApplicationId = appId.Value, RoleId = roleId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (exists == 0)
            return RoleOperationResult.Fail("Papel não encontrado.");

        var duplicate = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1 FROM aspnet_Roles WHERE ApplicationId = @ApplicationId AND LoweredRoleName = @LoweredRoleName AND RoleId <> @RoleId",
            new { ApplicationId = appId.Value, LoweredRoleName = lowered, RoleId = roleId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (duplicate != 0)
            return RoleOperationResult.Fail("Já existe outro papel com este nome.");

        await _connection.ExecuteAsync(new CommandDefinition(
            "UPDATE aspnet_Roles SET RoleName = @RoleName, LoweredRoleName = @LoweredRoleName, Description = @Description WHERE RoleId = @RoleId",
            new { RoleName = trimmed, LoweredRoleName = lowered, Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(), RoleId = roleId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        return RoleOperationResult.Ok("Papel atualizado.");
    }

    public async Task<RoleOperationResult> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return RoleOperationResult.Fail("Aplicação não configurada.");

        var roleName = await _connection.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT RoleName FROM aspnet_Roles WHERE ApplicationId = @ApplicationId AND RoleId = @RoleId",
            new { ApplicationId = appId.Value, RoleId = roleId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (roleName == null)
            return RoleOperationResult.Fail("Papel não encontrado.");

        if (ProtectedRoleNames.Contains(roleName))
            return RoleOperationResult.Fail("Este papel não pode ser removido.");

        await _connection.ExecuteAsync(new CommandDefinition("DELETE FROM ACTIVITY_ROLE WHERE ROLE_ID = @RoleId", new { RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await _connection.ExecuteAsync(new CommandDefinition("DELETE FROM aspnet_UsersInRoles WHERE RoleId = @RoleId", new { RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await _connection.ExecuteAsync(new CommandDefinition("DELETE FROM aspnet_Roles WHERE RoleId = @RoleId", new { RoleId = roleId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("Papel excluído: RoleId {RoleId}.", roleId);
        return RoleOperationResult.Ok("Papel excluído.");
    }

    private async Task<Guid?> GetApplicationIdAsync(CancellationToken ct)
    {
        const string sql = "SELECT ApplicationId FROM aspnet_Applications WHERE LoweredApplicationName = LOWER(@ApplicationName)";
        return await _connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { ApplicationName = _applicationName }, cancellationToken: ct)).ConfigureAwait(false);
    }
}
