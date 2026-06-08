using System.Data;
using Dapper;
using EUROERP.Application.Products;

namespace EUROERP.Infrastructure.Products;

public class ProductReferenceService : IProductReferenceService
{
    private readonly IDbConnection _connection;

    public ProductReferenceService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<IdNameDto>> GetMarketsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT m.PKId AS Id, m.NAME + ' - ' + cu.SYMBOL AS Name
            FROM MARKET m
            JOIN CURRENCY cu ON cu.PKId = m.CURRENCY_ID
            ORDER BY m.PKId";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetProductClassesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PKId AS Id, NAME AS Name FROM PRODUCT_CLASS ORDER BY PKId";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetProductGroupsByClassAsync(byte classId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT pg.PKId AS Id, pg.NAME AS Name
            FROM PRODUCT_GROUP pg
            WHERE pg.PRODUCT_CLASS_ID = @ClassId
            ORDER BY pg.NAME";
        var list = await _connection.QueryAsync<IdNameDto>(
            new CommandDefinition(sql, new { ClassId = classId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllProductGroupsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT pg.PKId AS Id, pg.NAME AS Name
            FROM PRODUCT_GROUP pg
            ORDER BY pg.PRODUCT_CLASS_ID, pg.NAME";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetFiscalClassesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PKId AS Id, CAST(PKId AS varchar) + ' - ' + NAME + ' - ' + VALUE AS Name FROM FISCAL_CLASS ORDER BY PKId";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PKId AS Id, CAST(PKId AS varchar) + ' - ' + NAME + ' - ' + SYMBOL AS Name FROM CURRENCY ORDER BY PKId";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetCstAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PKId AS Id, CAST(PKId AS varchar) + ' - ' + NAME AS Name FROM CST ORDER BY PKId";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetCstbAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PKId AS Id, CAST(PKId AS varchar) + ' - ' + NAME AS Name FROM CSTB ORDER BY PKId";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAnimalSizesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, ' Selecione' AS Name
            UNION ALL
            SELECT PKId AS Id, CAST(PKId AS varchar) + ' - ' + NAME AS Name FROM ANIMAL_SIZE ORDER BY Id";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PKId AS Id, CAST(PKId AS varchar) + ' - ' + SOCIAL_NAME AS Name FROM SUPPLIER ORDER BY SOCIAL_NAME";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetProductSuppliersForDropDownAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Todos' AS Name, 0 AS SortKey
            UNION
            SELECT DISTINCT psl.SUPPLIER_ID AS Id, s.SOCIAL_NAME AS Name, 1 AS SortKey
            FROM PRODUCT_SUPPLIER_LINK psl
            JOIN SUPPLIER s ON s.PKId = psl.SUPPLIER_ID
            ORDER BY SortKey, Name";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }
}
