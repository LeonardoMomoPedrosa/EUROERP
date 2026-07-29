namespace EUROERP.Application.Markets;

/// <summary>User-market assignments over MARKET / MARKET_USER (Epic 17).</summary>
public interface IMarketUserService
{
    Task<IReadOnlyList<MarketDto>> GetAllMarketsAsync(CancellationToken cancellationToken = default);

    /// <summary>Markets already assigned to the user.</summary>
    Task<IReadOnlyList<MarketDto>> GetUserMarketsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Markets not yet assigned to the user.</summary>
    Task<IReadOnlyList<MarketDto>> GetAvailableMarketsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddUserMarketAsync(Guid userId, byte marketId, CancellationToken cancellationToken = default);

    Task RemoveUserMarketAsync(Guid userId, byte marketId, CancellationToken cancellationToken = default);
}
