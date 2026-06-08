using EUROERP.Application.Products;

namespace EUROERP.Application.Suppliers;

public interface ISupplierReferenceService
{
    Task<IReadOnlyList<IdNameDto>> GetSupplierGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetStatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetCitiesByStateAsync(byte stateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetBanksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetDeliverySuppliersAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets STATE.PKId by UF code (e.g. "SP").</summary>
    Task<byte?> GetStateIdByCodeAsync(string uf, CancellationToken cancellationToken = default);
}
