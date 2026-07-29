namespace EUROERP.Application.RoleActivities;

/// <summary>Role with assigned activity ids (Epic 17).</summary>
public sealed class RoleWithActivitiesDto
{
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public List<int> ActvIds { get; init; } = new();
}
