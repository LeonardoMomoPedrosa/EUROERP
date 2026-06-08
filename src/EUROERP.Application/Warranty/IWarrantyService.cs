namespace EUROERP.Application.Warranty;

public interface IWarrantyService
{
    Task<int> CreateAsync(WarrantyCreateDto dto, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarrantyListItemDto>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    Task<WarrantyDto?> GetByIdAsync(int warrantyId, CancellationToken cancellationToken = default);
}
