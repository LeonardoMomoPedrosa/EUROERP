using System.Data;
using Dapper;
using EUROERP.Application.Markets;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.Markets;

public sealed class MarketUserService : IMarketUserService
{
    private readonly IDbConnection _connection;
    private readonly ILogger<MarketUserService> _logger;

    public MarketUserService(IDbConnection connection, ILogger<MarketUserService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MarketDto>> GetAllMarketsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT m.PKId AS Id, m.NAME + ' - ' + cu.SYMBOL AS Name
            FROM MARKET m
            INNER JOIN CURRENCY cu ON cu.PKId = m.CURRENCY_ID
            ORDER BY m.PKId";
        var rows = await _connection.QueryAsync<MarketDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<MarketDto>> GetUserMarketsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT m.PKId AS Id, m.NAME + ' - ' + cu.SYMBOL AS Name
            FROM MARKET_USER mu
            INNER JOIN MARKET m ON m.PKId = mu.MARKET_ID
            INNER JOIN CURRENCY cu ON cu.PKId = m.CURRENCY_ID
            WHERE mu.USER_ID = @UserId
            ORDER BY m.PKId";
        var rows = await _connection.QueryAsync<MarketDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<MarketDto>> GetAvailableMarketsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT m.PKId AS Id, m.NAME + ' - ' + cu.SYMBOL AS Name
            FROM MARKET m
            INNER JOIN CURRENCY cu ON cu.PKId = m.CURRENCY_ID
            WHERE NOT EXISTS (
                SELECT 1 FROM MARKET_USER mu
                WHERE mu.MARKET_ID = m.PKId AND mu.USER_ID = @UserId)
            ORDER BY m.PKId";
        var rows = await _connection.QueryAsync<MarketDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task AddUserMarketAsync(Guid userId, byte marketId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM MARKET_USER WHERE USER_ID = @UserId AND MARKET_ID = @MarketId)
                INSERT INTO MARKET_USER (USER_ID, MARKET_ID) VALUES (@UserId, @MarketId)";
        await _connection.ExecuteAsync(new CommandDefinition(
            sql, new { UserId = userId, MarketId = marketId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("Mercado {MarketId} atribuído ao usuário {UserId}.", marketId, userId);
    }

    public async Task RemoveUserMarketAsync(Guid userId, byte marketId, CancellationToken cancellationToken = default)
    {
        await _connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM MARKET_USER WHERE USER_ID = @UserId AND MARKET_ID = @MarketId",
            new { UserId = userId, MarketId = marketId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("Mercado {MarketId} removido do usuário {UserId}.", marketId, userId);
    }
}
