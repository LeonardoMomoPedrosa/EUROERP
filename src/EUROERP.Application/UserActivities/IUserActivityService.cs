using System.Security.Claims;

namespace EUROERP.Application.UserActivities;

/// <summary>User activity permissions (Epic 17). Activity codes are loaded at login and stored in claims; checks read the claim (no DB per request).</summary>
public interface IUserActivityService
{
    /// <summary>Claim type used to store comma-separated activity codes at login.</summary>
    const string ActivityCodesClaimType = "ActivityCodes";

    /// <summary>Loads all activity codes the user is allowed to perform (via roles and ACTIVITY_ROLE). Used at login to fill claims.</summary>
    Task<IReadOnlyList<string>> GetActivityCodesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the principal has the given activity code (reads from the ActivityCodes claim; no DB).</summary>
    bool UserHasActivity(ClaimsPrincipal user, string activityCode);
}
