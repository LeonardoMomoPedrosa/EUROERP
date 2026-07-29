namespace EUROERP.Application.Widgets;

/// <summary>Manages user dashboard shortcuts (pinned menu routes) stored in USER_SHORTCUT.</summary>
public interface IUserShortcutService
{
    /// <summary>Gets the user's selected shortcuts ordered by SortOrder.</summary>
    Task<IReadOnlyList<UserShortcutDto>> GetShortcutsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the user's shortcuts with the given list of routes. SortOrder is assigned by list index.</summary>
    Task SetShortcutsAsync(Guid userId, IReadOnlyList<string> routes, CancellationToken cancellationToken = default);
}
