namespace EUROERP.Application.Roles;

/// <summary>Role for list/edit (Epic 17).</summary>
public sealed class RoleDto
{
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public string? Description { get; init; }
}
