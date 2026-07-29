namespace EUROERP.Application.UserRoles;

/// <summary>User-role assignments (aspnet_UsersInRoles) and role names for login (Epic 17).</summary>
public interface IUserRolesService
{
    /// <summary>All users of the application with their assigned role ids.</summary>
    Task<IReadOnlyList<UserWithRolesDto>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Replace assigned roles for a user. Clears and re-inserts into aspnet_UsersInRoles.</summary>
    Task SetUserRolesAsync(Guid userId, IReadOnlyList<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>Role names for the user (for login claims).</summary>
    Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
