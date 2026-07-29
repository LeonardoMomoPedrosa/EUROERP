namespace EUROERP.Application.Widgets;

/// <summary>Known widget codes for the dashboard. Used in USER_WIDGET and when rendering widgets.</summary>
public static class WidgetCodes
{
    /// <summary>Daily sales (faturamento por dia) — vertical bar chart by day of current month.</summary>
    public const string DailySales = "DailySales";

    /// <summary>Daily sales accumulated (faturamento por dia acumulado) — line chart by day of current month.</summary>
    public const string DailySalesAccumulated = "DailySalesAccumulated";

    /// <summary>Last NFe generated — list of last NFes with links to PDF and XML.</summary>
    public const string LastNfesGenerated = "LastNfesGenerated";

    /// <summary>Shortcuts — user-chosen quick links to menu destinations on the dashboard.</summary>
    public const string Shortcuts = "Shortcuts";
}
