namespace EUROERP.Application.Master;

/// <summary>Outcome of an ad-hoc statement run from the Master SQL console (Epic 17).</summary>
public sealed class MasterSqlResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    /// <summary>True when the statement returned a result set.</summary>
    public bool IsQuery { get; init; }

    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } =
        Array.Empty<IReadOnlyDictionary<string, object?>>();

    public int RowsAffected { get; init; }

    public long ElapsedMs { get; init; }

    public static MasterSqlResult Fail(string message) => new() { Success = false, Message = message };
}
