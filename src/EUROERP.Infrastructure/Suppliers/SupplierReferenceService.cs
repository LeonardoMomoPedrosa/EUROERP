using System.Data;
using Dapper;
using EUROERP.Application.Products;
using EUROERP.Application.Suppliers;

namespace EUROERP.Infrastructure.Suppliers;

public class SupplierReferenceService : ISupplierReferenceService
{
    private readonly IDbConnection _connection;

    public SupplierReferenceService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<IdNameDto>> GetSupplierGroupsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name
            UNION ALL
            SELECT sg.PKId, sg.NAME
            FROM SUPPLIER_GROUP sg
            WHERE (sg.HIDDEN IS NULL OR sg.HIDDEN = '')
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetStatesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name
            UNION ALL
            SELECT s.PKId, s.CODE + ' - ' + s.NAME FROM STATE s
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetCitiesByStateAsync(byte stateId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name
            UNION ALL
            SELECT c.PKId, c.NAME
            FROM CITY c
            WHERE c.STATE_ID = @StateId
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<IdNameDto>(
            new CommandDefinition(sql, new { StateId = stateId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetBanksAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name
            UNION ALL
            SELECT b.PKId, b.NAME FROM BANK b
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name
            UNION ALL
            SELECT pm.PKId, pm.NAME FROM PAYMENT_METHOD pm
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetDeliverySuppliersAsync(CancellationToken cancellationToken = default)
    {
        const int deliverySupplierGroupId = 3;
        const string sql = @"
            SELECT s.PKId AS Id, s.SOCIAL_NAME AS Name
            FROM SUPPLIER s
            WHERE s.SUPPLIER_GROUP_ID = @GroupId AND s.ACTIVE = 'Y'
            ORDER BY s.SOCIAL_NAME";
        var list = await _connection.QueryAsync<IdNameDto>(
            new CommandDefinition(sql, new { GroupId = deliverySupplierGroupId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<byte?> GetStateIdByCodeAsync(string uf, CancellationToken cancellationToken = default)
    {
        var code = (uf ?? "").Trim().ToUpperInvariant();
        if (code.Length != 2)
            return null;

        const string sql = "SELECT s.PKId FROM STATE s WHERE s.CODE = @Code";
        var id = await _connection.QueryFirstOrDefaultAsync<byte?>(
            new CommandDefinition(sql, new { Code = code }, cancellationToken: cancellationToken));
        return id;
    }
}
