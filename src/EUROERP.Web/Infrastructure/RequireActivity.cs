using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace EUROERP.Web.Infrastructure;

/// <summary>Helper for activity-based access control (Epic 17). Returns false if user lacks the activity (and redirects to /acesso-negado).</summary>
public static class RequireActivity
{
    /// <summary>Returns true if the user has the activity; otherwise redirects to /acesso-negado and returns false.</summary>
    public static async Task<bool> RequireAsync(
        AuthenticationStateProvider authStateProvider,
        EUROERP.Application.UserActivities.IUserActivityService userActivityService,
        NavigationManager navigation,
        string activityCode)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        if (userActivityService.UserHasActivity(state.User, activityCode))
            return true;
        navigation.NavigateTo("/acesso-negado", forceLoad: true);
        return false;
    }
}
