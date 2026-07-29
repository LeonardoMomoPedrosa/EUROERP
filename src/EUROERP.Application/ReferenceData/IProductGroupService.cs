namespace EUROERP.Application.ReferenceData;

public interface IProductGroupService
{
    Task<IReadOnlyList<ProductGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(string name, int productClassId, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, string name, bool ignoreOrderDisc, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
