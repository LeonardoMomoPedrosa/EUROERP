namespace EUROERP.Application.Activities;

/// <summary>CRUD over SEC_ACTIVITY (Epic 17).</summary>
public interface IActivityService
{
    Task<IReadOnlyList<ActivityDto>> ListActivitiesAsync(CancellationToken cancellationToken = default);

    Task<ActivityOperationResult> CreateAsync(string code, string description, CancellationToken cancellationToken = default);

    Task<ActivityOperationResult> UpdateAsync(int actvId, string code, string description, CancellationToken cancellationToken = default);

    Task<ActivityOperationResult> DeleteAsync(int actvId, CancellationToken cancellationToken = default);
}
