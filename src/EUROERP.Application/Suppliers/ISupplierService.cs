namespace EUROERP.Application.Suppliers;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierSummaryDto>> GetListAsync(SupplierFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierSummaryDto>> GetSuggestionsAsync(string term, int groupId, int limit = 10, CancellationToken cancellationToken = default);
    Task<SupplierEditDto?> GetByIdAsync(int supplierId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(SupplierCreateDto dto, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(SupplierEditDto dto, CancellationToken cancellationToken = default);

    /// <summary>List suppliers by group IDs for mass-update grid.</summary>
    Task<IReadOnlyList<SupplierMassItemDto>> GetListByGroupIdsAsync(IEnumerable<int> groupIds, CancellationToken cancellationToken = default);

    /// <summary>Update one supplier row (mass-update). Returns (success, message).</summary>
    Task<(bool Success, string Message)> UpdateMassRowAsync(SupplierMassUpdateDto dto, CancellationToken cancellationToken = default);
}
