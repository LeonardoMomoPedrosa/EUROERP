using System.Data;
using Dapper;
using EUROERP.Application.Products;

namespace EUROERP.Infrastructure.Products;

public class ProductHistoryService : IProductHistoryService
{
    private readonly IDbConnection _connection;

    public ProductHistoryService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<ProductHistoryHeaderDto?> GetHeaderAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT p.NAME AS Name, az.NAME AS Size, p.STOCK AS Stock
            FROM PRODUCT p
            LEFT JOIN ANIMAL_SIZE az ON p.SIZE_ID = az.PKId
            WHERE p.PKId = @ProductId";
        var cmd = new CommandDefinition(sql, new { ProductId = productId }, cancellationToken: cancellationToken);
        return await _connection.QueryFirstOrDefaultAsync<ProductHistoryHeaderDto>(cmd);
    }

    public async Task<IReadOnlyList<ProductHistoryEntryDto>> GetTimelineAsync(int productId, int days, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                CONVERT(VARCHAR(10), sh.SYS_CREATION_DATE, 103) AS Date,
                CONVERT(VARCHAR(8), sh.SYS_CREATION_DATE, 108) AS Time,
                sh.USER_ID AS UserId,
                sh.PREVIOUS AS Previous,
                sh.QUANTITY AS Quantity,
                sh.MEMO AS Memo,
                c.FANTASY_NAME AS ClientName
            FROM STOCK_HISTORY sh
            LEFT JOIN CLIENT c ON c.PKId = sh.CLIENT_ID
            WHERE sh.PRODUCT_ID = @ProductId
              AND sh.QUANTITY <> 0
              AND sh.SYS_CREATION_DATE >= GETDATE() - @Days
            ORDER BY sh.SYS_CREATION_DATE";
        var cmd = new CommandDefinition(sql, new { ProductId = productId, Days = days }, cancellationToken: cancellationToken);
        var list = await _connection.QueryAsync<ProductHistoryEntryDto>(cmd);
        return list.ToList();
    }
}
