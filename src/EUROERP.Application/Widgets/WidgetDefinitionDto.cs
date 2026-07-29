namespace EUROERP.Application.Widgets;

/// <summary>Display definition for a dashboard widget (code, label and optional description).</summary>
public class WidgetDefinitionDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>Optional short description shown in Cadastro → Widgets to clarify what the widget does.</summary>
    public string? Description { get; set; }
}
