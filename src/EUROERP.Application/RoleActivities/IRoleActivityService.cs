using EUROERP.Application.Activities;

namespace EUROERP.Application.RoleActivities;

/// <summary>Role-activity assignments over ACTIVITY_ROLE (Epic 17).</summary>
public interface IRoleActivityService
{
    Task<IReadOnlyList<RoleWithActivitiesDto>> GetRolesWithActivitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the whole activity set of a role.</summary>
    Task SetRoleActivitiesAsync(Guid roleId, IReadOnlyList<int> actvIds, CancellationToken cancellationToken = default);

    /// <summary>Activities already granted to the role.</summary>
    Task<IReadOnlyList<ActivityDto>> GetActivitiesForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Activities not yet granted to the role.</summary>
    Task<IReadOnlyList<ActivityDto>> GetAvailableActivitiesForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task AddActivityToRoleAsync(Guid roleId, int actvId, CancellationToken cancellationToken = default);

    Task RemoveActivityFromRoleAsync(Guid roleId, int actvId, CancellationToken cancellationToken = default);
}
