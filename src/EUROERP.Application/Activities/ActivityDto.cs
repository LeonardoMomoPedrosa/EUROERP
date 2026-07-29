namespace EUROERP.Application.Activities;

/// <summary>Activity from SEC_ACTIVITY (Epic 17).</summary>
public sealed class ActivityDto
{
    public int ActvId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
