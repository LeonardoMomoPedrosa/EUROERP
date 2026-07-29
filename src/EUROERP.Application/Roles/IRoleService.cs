namespace EUROERP.Application.Roles;

/// <summary>Role CRUD using aspnet_Roles (Epic 17).</summary>
public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleOperationResult> CreateRoleAsync(string roleName, string? description, CancellationToken cancellationToken = default);

    Task<RoleOperationResult> UpdateRoleAsync(Guid roleId, string roleName, string? description, CancellationToken cancellationToken = default);

    Task<RoleOperationResult> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
