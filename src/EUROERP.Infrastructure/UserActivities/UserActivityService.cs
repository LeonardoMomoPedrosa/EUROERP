using System.Data;
using System.Security.Claims;
using Dapper;
using EUROERP.Application.UserActivities;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.UserActivities;

public sealed class UserActivityService : IUserActivityService
{
    private readonly IDbConnection _connection;
    private readonly ILogger<UserActivityService> _logger;

    public UserActivityService(IDbConnection connection, ILogger<UserActivityService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GetActivityCodesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DISTINCT a.CODE
            FROM SEC_ACTIVITY a
            INNER JOIN ACTIVITY_ROLE ar ON ar.ACTV_ID = a.PKId
            INNER JOIN aspnet_UsersInRoles ur ON ur.RoleId = ar.ROLE_ID
            WHERE ur.UserId = @UserId
            ORDER BY a.CODE";
        try
        {
            var codes = await _connection.QueryAsync<string>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return codes.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível carregar as atividades do usuário {UserId}.", userId);
            return Array.Empty<string>();
        }
    }

    public bool UserHasActivity(ClaimsPrincipal user, string activityCode)
    {
        if (user == null || string.IsNullOrWhiteSpace(activityCode))
            return false;
        var value = user.FindFirst(IUserActivityService.ActivityCodesClaimType)?.Value;
        if (string.IsNullOrEmpty(value))
            return false;
        var codes = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return codes.Contains(activityCode.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
