namespace EUROERP.Application.Clients;

public interface IClientService
{
    Task<IReadOnlyList<ClientSummaryDto>> GetListAsync(ClientFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientSummaryDto>> GetSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCarPlateSuggestionsAsync(string term, int limit = 15, CancellationToken cancellationToken = default);
    Task<ClientEditDto?> GetByIdAsync(int clientId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ClientCreateDto dto, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ClientEditDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientDiscountRowDto>> GetClientDiscountsAsync(int clientId, CancellationToken cancellationToken = default);
    Task SaveClientDiscountsAsync(int clientId, IReadOnlyList<ClientDiscountRowDto> rows, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientMassItemDto>> GetMassUpdateListAsync(string? name, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateMassRowAsync(ClientMassUpdateDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesAgentClientRowDto>> GetClientsBySalesAgentListAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetClientCreditBalanceAsync(int clientId, CancellationToken cancellationToken = default);
    Task ApplyClientCreditAsync(int clientId, decimal amount, string memo, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientCreditHistoryDto>> GetClientCreditHistoryAsync(int clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientCarDto>> GetCarsByClientAsync(int clientId, CancellationToken cancellationToken = default);
    Task<int> CreateCarAsync(int clientId, string plate, string description, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task UpdateCarAsync(int carId, string plate, string description, CancellationToken cancellationToken = default);
    Task DeleteCarAsync(int carId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HigienicOrderDto>> GetHigienicOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HigienicOrderByClientDto>> GetHigienicOrdersByClientAsync(CancellationToken cancellationToken = default);
    Task MarkHigienicProcessedAsync(int orderId, CancellationToken cancellationToken = default);
}
