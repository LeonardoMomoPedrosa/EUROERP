namespace EUROERP.Application.Config;

public interface ISysControlService
{
    Task<string?> GetValueAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetValuesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
    Task SetValueAsync(string code, string value, CancellationToken cancellationToken = default);
    Task SaveValuesAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default);
}
