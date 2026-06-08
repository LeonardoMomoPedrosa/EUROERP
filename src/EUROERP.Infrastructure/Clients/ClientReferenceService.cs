using System.Data;
using Dapper;
using EUROERP.Application.Clients;
using EUROERP.Application.Products;

namespace EUROERP.Infrastructure.Clients;

public class ClientReferenceService : IClientReferenceService
{
    private readonly IDbConnection _connection;

    public ClientReferenceService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<IdNameDto>> GetMarketsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name
            UNION ALL
            SELECT m.PKId, m.NAME
            FROM MARKET m
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name
            UNION ALL
            SELECT c.PKId, c.NAME
            FROM COUNTRY c
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<IdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<IdNameDto>> GetStatesAsync(int countryId = 1, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name
            UNION ALL
            SELECT s.PKId, s.CODE + ' - ' + s.NAME
            FROM STATE s
            WHERE s.COUNTRY_ID = @CountryId
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<IdNameDto>(
            new CommandDefinition(sql, new { CountryId = countryId }, cancellationToken: cancellationToken));
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

    public async Task<IReadOnlyList<PaymentMethodRefDto>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 0 AS Id, 'Selecione' AS Name, CAST(0 AS tinyint) AS MaxTerms, CAST(0 AS decimal(18,4)) AS MinAmount
            UNION ALL
            SELECT pm.PKId, pm.NAME, ISNULL(pm.MAX_TERMS, 0), ISNULL(pm.MIN_AMOUNT, 0)
            FROM PAYMENT_METHOD pm
            ORDER BY 1, 2";
        var list = await _connection.QueryAsync<PaymentMethodRefDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<PaymentMethodRefDto?> GetPaymentMethodByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id < 0)
            return null;
        if (id == 0)
            return new PaymentMethodRefDto { Id = 0, Name = "Selecione", MaxTerms = 0, MinAmount = 0 };

        const string sql = @"
            SELECT pm.PKId AS Id, pm.NAME AS Name, ISNULL(pm.MAX_TERMS, 0) AS MaxTerms, ISNULL(pm.MIN_AMOUNT, 0) AS MinAmount
            FROM PAYMENT_METHOD pm
            WHERE pm.PKId = @Id";
        return await _connection.QueryFirstOrDefaultAsync<PaymentMethodRefDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PaymentSubMethodDto>> GetPaymentSubMethodsAsync(int paymentMethodId, CancellationToken cancellationToken = default)
    {
        if (paymentMethodId <= 0)
            return [];

        const string sql = @"
            SELECT psm.PKId AS Id,
                   psm.NAME AS Name,
                   psm.PAYMENT_METHOD_ID AS PaymentMethodId,
                   psm.MAX_TERMS AS MaxTerms,
                   psm.MIN_AMOUNT AS MinAmount,
                   psm.ALLOW_FRONT AS AllowFront
            FROM PAYMENT_SUB_METHOD psm
            WHERE psm.PAYMENT_METHOD_ID = @PaymentMethodId
            ORDER BY psm.PKId";
        var list = await _connection.QueryAsync<PaymentSubMethodDto>(
            new CommandDefinition(sql, new { PaymentMethodId = paymentMethodId }, cancellationToken: cancellationToken));
        return [.. list];
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

    public async Task<IReadOnlyList<UserIdNameDto>> GetSalesAgentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CONVERT(nvarchar(36), u.UserId) AS Id, u.UserName AS Name
            FROM aspnet_Users u
            INNER JOIN aspnet_UsersInRoles ur ON u.UserId = ur.UserId
            INNER JOIN aspnet_Roles r ON ur.RoleId = r.RoleId
            WHERE r.LoweredRoleName = 'vendas'
            ORDER BY u.UserName";
        var list = await _connection.QueryAsync<UserIdNameDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
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

    public async Task<byte?> GetStateIdByNameAsync(string stateName, CancellationToken cancellationToken = default)
    {
        var name = (stateName ?? "").Trim();
        if (string.IsNullOrEmpty(name))
            return null;

        const string sql = "SELECT s.PKId FROM STATE s WHERE s.NAME = @Name";
        var id = await _connection.QueryFirstOrDefaultAsync<byte?>(
            new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken));
        return id;
    }
}
