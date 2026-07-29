namespace EUROERP.Application.Master;

/// <summary>Ad-hoc SQL execution restricted to the Master SQL console (Epic 17).</summary>
public interface IMasterSqlService
{
    /// <summary>True when the statement is a read-only SELECT (or CTE ending in SELECT).</summary>
    bool IsSelect(string sql);

    /// <summary>Runs the statement. SELECT statements return rows; anything else returns the affected row count.</summary>
    Task<MasterSqlResult> ExecuteAsync(string sql, string userName, CancellationToken cancellationToken = default);
}
