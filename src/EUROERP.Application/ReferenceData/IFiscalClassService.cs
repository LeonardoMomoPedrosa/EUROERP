namespace EUROERP.Application.ReferenceData;

public interface IFiscalClassService
{
    Task<IReadOnlyList<FiscalClassDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(int id, string value, decimal? ipi, bool icmsst, string name, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, string value, decimal? ipi, bool icmsst, string name, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
