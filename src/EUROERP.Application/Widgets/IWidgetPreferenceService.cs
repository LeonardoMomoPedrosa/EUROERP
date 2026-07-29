namespace EUROERP.Application.Widgets;

/// <summary>Manages which widgets each user has enabled on the dashboard.</summary>
public interface IWidgetPreferenceService
{
    /// <summary>Returns the list of widget codes enabled for the user.</summary>
    Task<IReadOnlyList<string>> GetEnabledWidgetCodesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the user's enabled widgets with the given list.</summary>
    Task SetEnabledWidgetsAsync(Guid userId, IReadOnlyList<string> widgetCodes, CancellationToken cancellationToken = default);

    /// <summary>Returns all available widget definitions (code + display label).</summary>
    IReadOnlyList<WidgetDefinitionDto> GetAvailableWidgets();
}
