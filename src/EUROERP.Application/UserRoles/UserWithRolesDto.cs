namespace EUROERP.Application.UserRoles;

/// <summary>User with assigned role ids (Epic 17).</summary>
public sealed class UserWithRolesDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public List<Guid> RoleIds { get; init; } = new();
}
