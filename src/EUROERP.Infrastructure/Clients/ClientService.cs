using System.Data;
using Dapper;
using EUROERP.Application.Address;
using EUROERP.Application.Clients;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.Clients;

public class ClientService : IClientService
{
    private readonly IDbConnection _connection;
    private readonly ICityResolutionService _cityResolution;

    public ClientService(IDbConnection connection, ICityResolutionService cityResolution)
    {
        _connection = connection;
        _cityResolution = cityResolution;
    }

    public async Task<IReadOnlyList<ClientSummaryDto>> GetListAsync(ClientFilterDto filter, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT TOP 15
                c.PKId AS Id,
                c.SOCIAL_NAME AS SocialName,
                c.FANTASY_NAME AS FantasyName,
                ct.NAME AS City,
                st.CODE AS State,
                c.PHONE1 + ISNULL(', ' + c.PHONE2, '') + ISNULL(', ' + c.PHONE3, '') AS Phone,
                c.FAX_NO AS FaxNo,
                (SELECT TOP 1 u.UserName FROM aspnet_Users u INNER JOIN CLIENT_SALES_AGENTS_LINK cs ON u.UserId = cs.USER_ID WHERE cs.CLIENT_ID = c.PKId) AS Sales
            FROM CLIENT c
            JOIN STATE st ON c.ADDRESS_STATE_ID = st.PKId
            JOIN CITY ct ON c.ADDRESS_CITY_ID = ct.PKId
            WHERE c.ACTIVE = 'Y'";

        if (!string.IsNullOrWhiteSpace(filter.Name))
            sql += " AND (c.SOCIAL_NAME LIKE @NameLike OR c.FANTASY_NAME LIKE @NameLike)";

        if (!string.IsNullOrWhiteSpace(filter.Cnpjpf))
            sql += " AND (REPLACE(REPLACE(REPLACE(c.CNPJPF, '.', ''), '-', ''), '/', '') LIKE @CnpjpfLike)";

        sql += " ORDER BY c.SOCIAL_NAME";

        var nameLike = string.IsNullOrWhiteSpace(filter.Name) ? null : $"%{filter.Name.Trim()}%";
        var cnpjpfDigits = string.IsNullOrWhiteSpace(filter.Cnpjpf) ? null : new string(filter.Cnpjpf.Trim().Where(char.IsDigit).ToArray());
        var cnpjpfLike = string.IsNullOrEmpty(cnpjpfDigits) ? null : $"%{cnpjpfDigits}%";

        var list = await _connection.QueryAsync<ClientSummaryDto>(
            new CommandDefinition(sql, new { NameLike = nameLike, CnpjpfLike = cnpjpfLike }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<ClientSummaryDto>> GetSuggestionsAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        var t = (term ?? "").Trim();
        if (string.IsNullOrEmpty(t))
            return new List<ClientSummaryDto>();

        var nameLike = $"%{t}%";
        const string sql = @"
            SELECT TOP (@Limit)
                c.PKId AS Id,
                c.SOCIAL_NAME AS SocialName,
                c.FANTASY_NAME AS FantasyName,
                ct.NAME AS City,
                st.CODE AS State
            FROM CLIENT c
            LEFT JOIN CITY ct ON c.ADDRESS_CITY_ID = ct.PKId
            LEFT JOIN STATE st ON c.ADDRESS_STATE_ID = st.PKId
            WHERE c.ACTIVE = 'Y'
            AND (c.SOCIAL_NAME LIKE @NameLike OR (c.FANTASY_NAME IS NOT NULL AND c.FANTASY_NAME LIKE @NameLike))
            ORDER BY c.SOCIAL_NAME";
        var list = await _connection.QueryAsync<ClientSummaryDto>(
            new CommandDefinition(sql, new { NameLike = nameLike, Limit = limit }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<string>> GetCarPlateSuggestionsAsync(string term, int limit = 15, CancellationToken cancellationToken = default)
    {
        var t = (term ?? "").Trim();
        if (t.Length < 2)
            return Array.Empty<string>();

        const string sql = @"
            SELECT DISTINCT TOP (@Limit) PLATE
            FROM CAR
            WHERE PLATE LIKE @Like
            ORDER BY PLATE";
        var list = await _connection.QueryAsync<string>(
            new CommandDefinition(sql, new { Like = $"%{t}%", Limit = limit }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<ClientEditDto?> GetByIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        const string sqlClient = @"
            SELECT
                c.PKId AS Id,
                c.PERSON_TYPE AS PersonType,
                c.CNPJPF AS Cnpjpf,
                c.STATE_INSCR AS StateInscr,
                c.CONTACT AS Contact,
                c.SOCIAL_NAME AS SocialName,
                c.FANTASY_NAME AS FantasyName,
                c.MARKET_ID AS MarketId,
                c.ADDRESS_COUNTRY_ID AS AddressCountryId,
                c.ADDRESS_STREET AS AddressStreet,
                c.ADDRESS_NUMBER AS AddressNumber,
                c.ADDRESS_COMPLEMENT AS AddressComplement,
                c.ADDRESS_BLOCK AS AddressBlock,
                c.ADDRESS_ZIPCODE AS AddressZipCode,
                c.ADDRESS_STATE_ID AS AddressStateId,
                c.ADDRESS_CITY_ID AS AddressCityId,
                ct.NAME AS AddressCityName,
                CONVERT(varchar(20), ct.C_MUN) AS AddressCityIbge,
                c.PHONE1 AS Phone1,
                c.PHONE2 AS Phone2,
                c.PHONE3 AS Phone3,
                c.FAX_NO AS FaxNo,
                c.CELULAR AS Celular,
                c.EMAIL AS Email,
                c.PAYMENT_METHOD_ID AS PaymentMethodId,
                c.PAYMENT_METHOD_ID2 AS PaymentMethodId2,
                c.PAYMENT_METHOD_ID3 AS PaymentMethodId3,
                c.AVG_PAYTERM AS AvgPayTerm,
                c.LIMIT_AMOUNT AS LimitAmount,
                c.BIRTHDAY AS Birthday,
                c.BIRTHMONTH AS BirthMonth,
                c.BILL_ADDRESS_STREET AS BillAddressStreet,
                c.BILL_ADDRESS_BLOCK AS BillAddressBlock,
                c.BILL_ADDRESS_NUMBER AS BillAddressNumber,
                c.BILL_ADDRESS_ZIPCODE AS BillAddressZipCode,
                c.BILL_ADDRESS_INDICATOR AS BillAddressIndicator,
                c.OBS AS Obs
            FROM CLIENT c
            LEFT JOIN CITY ct ON c.ADDRESS_CITY_ID = ct.PKId
            WHERE c.PKId = @ClientId AND c.ACTIVE = 'Y'";

        var client = await _connection.QuerySingleOrDefaultAsync<ClientEditDto>(
            new CommandDefinition(sqlClient, new { ClientId = clientId }, cancellationToken: cancellationToken));
        if (client == null)
            return null;

        const string sqlDelivery = "SELECT DELIVERY_SUPPLIER_ID AS Id FROM CLIENT_DELIVERY_SUPPLIER_LINK WHERE CLIENT_ID = @ClientId";
        var deliveryIds = await _connection.QueryAsync<int>(
            new CommandDefinition(sqlDelivery, new { ClientId = clientId }, cancellationToken: cancellationToken));
        client.DeliverySupplierIds = deliveryIds.ToList();

        const string sqlSales = "SELECT CONVERT(nvarchar(36), USER_ID) AS Id FROM CLIENT_SALES_AGENTS_LINK WHERE CLIENT_ID = @ClientId";
        var salesIds = await _connection.QueryAsync<string>(
            new CommandDefinition(sqlSales, new { ClientId = clientId }, cancellationToken: cancellationToken));
        client.SalesAgentIds = salesIds.ToList();

        return client;
    }

    public async Task<int> CreateAsync(ClientCreateDto dto, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var appId = (applicationId ?? "").Length > 8 ? (applicationId ?? "").Substring(0, 8) : (applicationId ?? "EUROERP");
        var usrId = (userId ?? "").Length > 20 ? (userId ?? "").Substring(0, 20) : (userId ?? "SYS");

        var addressCityId = dto.AddressCityId;
        if (!string.IsNullOrWhiteSpace(dto.AddressCityName) && dto.AddressStateId > 0)
        {
            addressCityId = await _cityResolution.ResolveOrCreateCityAsync(
                dto.AddressStateId, dto.AddressCityName, dto.AddressCityIbge, null, cancellationToken);
        }

        const string sqlInsert = @"
            INSERT INTO CLIENT (
                CNPJPF, PERSON_TYPE, FANTASY_NAME, SOCIAL_NAME,
                ADDRESS_STREET, ADDRESS_BLOCK, ADDRESS_NUMBER, ADDRESS_COMPLEMENT,
                ADDRESS_STATE_ID, ADDRESS_CITY_ID, ADDRESS_ZIPCODE,
                PHONE1, PHONE2, PHONE3, FAX_NO, CELULAR, STATE_INSCR, OBS, CONTACT,
                PAYMENT_METHOD_ID, PAYMENT_METHOD_ID2, PAYMENT_METHOD_ID3,
                EMAIL, BIRTHDAY, BIRTHMONTH, AVG_PAYTERM, LIMIT_AMOUNT,
                BILL_ADDRESS_STREET, BILL_ADDRESS_BLOCK, BILL_ADDRESS_NUMBER, BILL_ADDRESS_ZIPCODE, BILL_ADDRESS_INDICATOR,
                SYS_CREATION_DATE, APPLICATION_ID, USER_ID, MARKET_ID, ADDRESS_COUNTRY_ID, LEDGE)
            VALUES (
                @Cnpjpf, @PersonType, @FantasyName, @SocialName,
                @AddressStreet, @AddressBlock, @AddressNumber, @AddressComplement,
                @AddressStateId, @AddressCityId, @AddressZipCode,
                @Phone1, @Phone2, @Phone3, @FaxNo, @Celular, @StateInscr, @Obs, @Contact,
                @PaymentMethodId, @PaymentMethodId2, @PaymentMethodId3,
                @Email, @Birthday, @BirthMonth, @AvgPayTerm, @LimitAmount,
                @BillAddressStreet, @BillAddressBlock, @BillAddressNumber, @BillAddressZipCode, @BillAddressIndicator,
                GETDATE(), @ApplicationId, @UserId, @MarketId, @AddressCountryId, 'Y');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        var newId = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlInsert, new
            {
                dto.Cnpjpf,
                dto.PersonType,
                FantasyName = dto.FantasyName ?? (object)DBNull.Value,
                dto.SocialName,
                AddressStreet = dto.AddressStreet ?? (object)DBNull.Value,
                AddressBlock = dto.AddressBlock ?? (object)DBNull.Value,
                AddressNumber = dto.AddressNumber ?? (object)DBNull.Value,
                AddressComplement = dto.AddressComplement ?? (object)DBNull.Value,
                dto.AddressStateId,
                addressCityId,
                AddressZipCode = dto.AddressZipCode ?? (object)DBNull.Value,
                Phone1 = dto.Phone1 ?? (object)DBNull.Value,
                Phone2 = dto.Phone2 ?? (object)DBNull.Value,
                Phone3 = dto.Phone3 ?? (object)DBNull.Value,
                FaxNo = dto.FaxNo ?? (object)DBNull.Value,
                Celular = dto.Celular ?? (object)DBNull.Value,
                StateInscr = dto.StateInscr ?? (object)DBNull.Value,
                Obs = dto.Obs ?? (object)DBNull.Value,
                Contact = dto.Contact ?? (object)DBNull.Value,
                PaymentMethodId = dto.PaymentMethodId ?? (object)DBNull.Value,
                PaymentMethodId2 = dto.PaymentMethodId2 ?? (object)DBNull.Value,
                PaymentMethodId3 = dto.PaymentMethodId3 ?? (object)DBNull.Value,
                Email = dto.Email ?? (object)DBNull.Value,
                Birthday = dto.Birthday ?? (object)DBNull.Value,
                BirthMonth = dto.BirthMonth ?? (object)DBNull.Value,
                AvgPayTerm = dto.AvgPayTerm ?? (object)DBNull.Value,
                LimitAmount = dto.LimitAmount ?? (object)DBNull.Value,
                BillAddressStreet = dto.BillAddressStreet ?? (object)DBNull.Value,
                BillAddressBlock = dto.BillAddressBlock ?? (object)DBNull.Value,
                BillAddressNumber = dto.BillAddressNumber ?? (object)DBNull.Value,
                BillAddressZipCode = dto.BillAddressZipCode ?? (object)DBNull.Value,
                dto.BillAddressIndicator,
                ApplicationId = appId,
                UserId = usrId,
                dto.MarketId,
                dto.AddressCountryId
            }, cancellationToken: cancellationToken));

        const string sqlLinkDelivery = "INSERT INTO CLIENT_DELIVERY_SUPPLIER_LINK (CLIENT_ID, DELIVERY_SUPPLIER_ID) VALUES (@ClientId, @DeliverySupplierId)";
        foreach (var delId in dto.DeliverySupplierIds)
        {
            if (delId > 0)
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlLinkDelivery, new { ClientId = newId, DeliverySupplierId = delId }, cancellationToken: cancellationToken));
            }
        }

        const string sqlLinkSales = "INSERT INTO CLIENT_SALES_AGENTS_LINK (CLIENT_ID, USER_ID) VALUES (@ClientId, @UserId)";
        foreach (var uid in dto.SalesAgentIds)
        {
            if (!string.IsNullOrWhiteSpace(uid) && Guid.TryParse(uid, out _))
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlLinkSales, new { ClientId = newId, UserId = uid }, cancellationToken: cancellationToken));
            }
        }

        return newId;
    }

    public async Task<bool> UpdateAsync(ClientEditDto dto, CancellationToken cancellationToken = default)
    {
        var addressCityId = dto.AddressCityId;
        if (!string.IsNullOrWhiteSpace(dto.AddressCityName) && dto.AddressStateId > 0)
        {
            addressCityId = await _cityResolution.ResolveOrCreateCityAsync(
                dto.AddressStateId, dto.AddressCityName, dto.AddressCityIbge, null, cancellationToken);
        }

        const string sqlUpdate = @"
            UPDATE CLIENT SET
                PERSON_TYPE = @PersonType,
                CNPJPF = @Cnpjpf,
                STATE_INSCR = @StateInscr,
                CONTACT = @Contact,
                SOCIAL_NAME = @SocialName,
                FANTASY_NAME = @FantasyName,
                MARKET_ID = @MarketId,
                ADDRESS_STREET = @AddressStreet,
                ADDRESS_NUMBER = @AddressNumber,
                ADDRESS_COMPLEMENT = @AddressComplement,
                ADDRESS_BLOCK = @AddressBlock,
                ADDRESS_ZIPCODE = @AddressZipCode,
                ADDRESS_STATE_ID = @AddressStateId,
                ADDRESS_CITY_ID = @AddressCityId,
                PHONE1 = @Phone1,
                PHONE2 = @Phone2,
                PHONE3 = @Phone3,
                FAX_NO = @FaxNo,
                CELULAR = @Celular,
                EMAIL = @Email,
                PAYMENT_METHOD_ID = @PaymentMethodId,
                PAYMENT_METHOD_ID2 = @PaymentMethodId2,
                PAYMENT_METHOD_ID3 = @PaymentMethodId3,
                AVG_PAYTERM = @AvgPayTerm,
                LIMIT_AMOUNT = @LimitAmount,
                BIRTHDAY = @Birthday,
                BIRTHMONTH = @BirthMonth,
                BILL_ADDRESS_STREET = @BillAddressStreet,
                BILL_ADDRESS_BLOCK = @BillAddressBlock,
                BILL_ADDRESS_NUMBER = @BillAddressNumber,
                BILL_ADDRESS_ZIPCODE = @BillAddressZipCode,
                BILL_ADDRESS_INDICATOR = @BillAddressIndicator,
                OBS = @Obs,
                SYS_UPDATE_DATE = GETDATE()
            WHERE PKId = @Id";

        var rows = await _connection.ExecuteAsync(
            new CommandDefinition(sqlUpdate, new
            {
                dto.Id,
                dto.PersonType,
                dto.Cnpjpf,
                StateInscr = dto.StateInscr ?? (object)DBNull.Value,
                Contact = dto.Contact ?? (object)DBNull.Value,
                dto.SocialName,
                FantasyName = dto.FantasyName ?? (object)DBNull.Value,
                dto.MarketId,
                AddressStreet = dto.AddressStreet ?? (object)DBNull.Value,
                AddressNumber = dto.AddressNumber ?? (object)DBNull.Value,
                AddressComplement = dto.AddressComplement ?? (object)DBNull.Value,
                AddressBlock = dto.AddressBlock ?? (object)DBNull.Value,
                AddressZipCode = dto.AddressZipCode ?? (object)DBNull.Value,
                dto.AddressStateId,
                addressCityId,
                Phone1 = dto.Phone1 ?? (object)DBNull.Value,
                Phone2 = dto.Phone2 ?? (object)DBNull.Value,
                Phone3 = dto.Phone3 ?? (object)DBNull.Value,
                FaxNo = dto.FaxNo ?? (object)DBNull.Value,
                Celular = dto.Celular ?? (object)DBNull.Value,
                Email = dto.Email ?? (object)DBNull.Value,
                PaymentMethodId = dto.PaymentMethodId ?? (object)DBNull.Value,
                PaymentMethodId2 = dto.PaymentMethodId2 ?? (object)DBNull.Value,
                PaymentMethodId3 = dto.PaymentMethodId3 ?? (object)DBNull.Value,
                AvgPayTerm = dto.AvgPayTerm ?? (object)DBNull.Value,
                LimitAmount = dto.LimitAmount ?? (object)DBNull.Value,
                Birthday = dto.Birthday ?? (object)DBNull.Value,
                BirthMonth = dto.BirthMonth ?? (object)DBNull.Value,
                BillAddressStreet = dto.BillAddressStreet ?? (object)DBNull.Value,
                BillAddressBlock = dto.BillAddressBlock ?? (object)DBNull.Value,
                BillAddressNumber = dto.BillAddressNumber ?? (object)DBNull.Value,
                BillAddressZipCode = dto.BillAddressZipCode ?? (object)DBNull.Value,
                dto.BillAddressIndicator,
                Obs = dto.Obs ?? (object)DBNull.Value
            }, cancellationToken: cancellationToken));

        if (rows == 0)
            return false;

        await _connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM CLIENT_DELIVERY_SUPPLIER_LINK WHERE CLIENT_ID = @ClientId", new { ClientId = dto.Id }, cancellationToken: cancellationToken));
        const string sqlLinkDelivery = "INSERT INTO CLIENT_DELIVERY_SUPPLIER_LINK (CLIENT_ID, DELIVERY_SUPPLIER_ID) VALUES (@ClientId, @DeliverySupplierId)";
        foreach (var delId in dto.DeliverySupplierIds)
        {
            if (delId > 0)
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlLinkDelivery, new { ClientId = dto.Id, DeliverySupplierId = delId }, cancellationToken: cancellationToken));
            }
        }

        await _connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM CLIENT_SALES_AGENTS_LINK WHERE CLIENT_ID = @ClientId", new { ClientId = dto.Id }, cancellationToken: cancellationToken));
        const string sqlLinkSales = "INSERT INTO CLIENT_SALES_AGENTS_LINK (CLIENT_ID, USER_ID) VALUES (@ClientId, @UserId)";
        foreach (var uid in dto.SalesAgentIds)
        {
            if (!string.IsNullOrWhiteSpace(uid) && Guid.TryParse(uid, out _))
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlLinkSales, new { ClientId = dto.Id, UserId = uid }, cancellationToken: cancellationToken));
            }
        }

        return true;
    }

    public async Task<IReadOnlyList<ClientDiscountRowDto>> GetClientDiscountsAsync(int clientId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT pg.PKId AS ProductGroupId, pg.NAME AS ProductGroupName, ISNULL(d.DISCOUNT, 0) AS Discount
            FROM PRODUCT_GROUP pg
            LEFT JOIN DISCOUNT d ON d.PRODUCT_GROUP_ID = pg.PKId AND d.CLIENT_ID = @ClientId
            ORDER BY pg.PRODUCT_CLASS_ID, pg.NAME";
        var list = await _connection.QueryAsync<ClientDiscountRowDto>(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task SaveClientDiscountsAsync(int clientId, IReadOnlyList<ClientDiscountRowDto> rows, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using var trx = _connection.BeginTransaction();
        try
        {
            await _connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM DISCOUNT WHERE CLIENT_ID = @ClientId", new { ClientId = clientId }, transaction: trx, cancellationToken: cancellationToken));

            const string insertSql = @"
                INSERT INTO DISCOUNT (CLIENT_ID, PRODUCT_GROUP_ID, DISCOUNT)
                VALUES (@ClientId, @ProductGroupId, @Discount)";
            foreach (var row in (rows ?? Array.Empty<ClientDiscountRowDto>()).Where(r => r.Discount > 0))
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(insertSql, new { ClientId = clientId, row.ProductGroupId, row.Discount }, transaction: trx, cancellationToken: cancellationToken));
            }
            trx.Commit();
        }
        catch
        {
            trx.Rollback();
            throw;
        }
    }

    public async Task<IReadOnlyList<ClientMassItemDto>> GetMassUpdateListAsync(string? name, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT TOP 100
                c.PKId AS Id,
                c.SOCIAL_NAME AS SocialName,
                c.FANTASY_NAME AS FantasyName,
                ISNULL(c.LIMIT_AMOUNT, 0) AS LimitAmount,
                ISNULL(c.AVG_PAYTERM, 0) AS AvgPayTerm,
                ISNULL(c.PAYMENT_METHOD_ID, 0) AS PaymentMethodId,
                ISNULL(c.PAYMENT_METHOD_ID2, 0) AS PaymentMethodId2,
                (SELECT TOP 1 u.UserName FROM aspnet_Users u INNER JOIN CLIENT_SALES_AGENTS_LINK cs ON u.UserId = cs.USER_ID WHERE cs.CLIENT_ID = c.PKId) AS SalesAgentUserName,
                (SELECT TOP 1 CONVERT(nvarchar(36), cs.USER_ID) FROM CLIENT_SALES_AGENTS_LINK cs WHERE cs.CLIENT_ID = c.PKId) AS SalesAgentUserId
            FROM CLIENT c
            WHERE c.ACTIVE = 'Y'";

        if (!string.IsNullOrWhiteSpace(name))
            sql += " AND (c.SOCIAL_NAME LIKE @NameLike OR c.FANTASY_NAME LIKE @NameLike)";

        sql += " ORDER BY c.SOCIAL_NAME";

        var nameLike = string.IsNullOrWhiteSpace(name) ? null : $"%{name.Trim()}%";
        var list = await _connection.QueryAsync<ClientMassItemDto>(
            new CommandDefinition(sql, new { NameLike = nameLike }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<(bool Success, string Message)> UpdateMassRowAsync(ClientMassUpdateDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            const string sqlUpdate = @"
                UPDATE CLIENT SET
                    PAYMENT_METHOD_ID = @PaymentMethodId,
                    PAYMENT_METHOD_ID2 = @PaymentMethodId2,
                    AVG_PAYTERM = @AvgPayTerm,
                    LIMIT_AMOUNT = @LimitAmount,
                    SYS_UPDATE_DATE = GETDATE()
                WHERE PKId = @ClientId";

            await _connection.ExecuteAsync(
                new CommandDefinition(sqlUpdate, dto, cancellationToken: cancellationToken));

            await _connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM CLIENT_SALES_AGENTS_LINK WHERE CLIENT_ID = @ClientId", new { dto.ClientId }, cancellationToken: cancellationToken));

            if (!string.IsNullOrWhiteSpace(dto.SalesAgentUserId) && Guid.TryParse(dto.SalesAgentUserId, out _))
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(
                        "INSERT INTO CLIENT_SALES_AGENTS_LINK (CLIENT_ID, USER_ID) VALUES (@ClientId, @UserId)",
                        new { dto.ClientId, UserId = dto.SalesAgentUserId },
                        cancellationToken: cancellationToken));
            }

            return (true, "Atualizado");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<IReadOnlyList<SalesAgentClientRowDto>> GetClientsBySalesAgentListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                au.UserName AS SalesAgentUserName,
                c.PKId AS ClientId,
                c.SOCIAL_NAME AS SocialName,
                c.FANTASY_NAME AS FantasyName,
                c.PHONE1 AS Phone1,
                cy.NAME AS City,
                st.CODE AS State
            FROM aspnet_Users au
            JOIN CLIENT_SALES_AGENTS_LINK cl ON cl.USER_ID = au.UserId
            JOIN CLIENT c ON c.PKId = cl.CLIENT_ID
            JOIN STATE st ON c.ADDRESS_STATE_ID = st.PKId
            JOIN CITY cy ON c.ADDRESS_CITY_ID = cy.PKId
            WHERE c.ACTIVE = 'Y'
            ORDER BY au.UserName, c.SOCIAL_NAME";
        var list = await _connection.QueryAsync<SalesAgentClientRowDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<decimal> GetClientCreditBalanceAsync(int clientId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT ISNULL(CREDIT, 0) FROM CLIENT WHERE PKId = @ClientId";
        return await _connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));
    }

    public async Task ApplyClientCreditAsync(int clientId, decimal amount, string memo, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        if (clientId <= 0)
            throw new ArgumentException("Cliente inválido.");

        var appId = (applicationId ?? "").Length > 8 ? applicationId[..8] : (applicationId ?? "EUROERP");
        var usrId = (userId ?? "").Length > 20 ? userId[..20] : (userId ?? "SYS");

        await _connection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE CLIENT SET CREDIT = ISNULL(CREDIT, 0) + @Amount WHERE PKId = @ClientId",
                new { Amount = amount, ClientId = clientId },
                cancellationToken: cancellationToken));

        await _connection.ExecuteAsync(
            new CommandDefinition(
                @"INSERT INTO CLIENT_CREDIT_HIST (SYS_CREATION_DATE, APPLICATION_ID, USER_ID, AMOUNT, CLIENT_ID, ORDER_ID, MEMO)
                  VALUES (GETDATE(), @ApplicationId, @UserId, @Amount, @ClientId, NULL, @Memo)",
                new { ApplicationId = appId, UserId = usrId, Amount = amount, ClientId = clientId, Memo = memo ?? "" },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ClientCreditHistoryDto>> GetClientCreditHistoryAsync(int clientId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT SYS_CREATION_DATE AS CreatedAt, USER_ID AS UserId, AMOUNT AS Amount, ORDER_ID AS OrderId, MEMO AS Memo
            FROM CLIENT_CREDIT_HIST
            WHERE CLIENT_ID = @ClientId
            ORDER BY SYS_CREATION_DATE DESC";
        var list = await _connection.QueryAsync<ClientCreditHistoryDto>(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<ClientCarDto>> GetCarsByClientAsync(int clientId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT PKId AS Id, CLIENT_ID AS ClientId, PLATE AS Plate, DESCRIPTION AS Description, LAST_KM AS LastKm
            FROM CAR WHERE CLIENT_ID = @ClientId ORDER BY DESCRIPTION";
        var list = await _connection.QueryAsync<ClientCarDto>(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<int> CreateCarAsync(int clientId, string plate, string description, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var appId = (applicationId ?? "").Length > 20 ? applicationId[..20] : (applicationId ?? "EUROERP");
        var usrId = (userId ?? "").Length > 20 ? userId[..20] : (userId ?? "SYS");
        const string sql = @"
            INSERT INTO CAR (DESCRIPTION, PLATE, LAST_KM, SYS_CREATION_DATE, USER_ID, APPLICATION_ID, CLIENT_ID)
            VALUES (@Description, @Plate, 0, GETDATE(), @UserId, @ApplicationId, @ClientId);
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                Description = description,
                Plate = plate,
                UserId = usrId,
                ApplicationId = appId,
                ClientId = clientId
            }, cancellationToken: cancellationToken));
    }

    public async Task UpdateCarAsync(int carId, string plate, string description, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE CAR SET PLATE = @Plate, DESCRIPTION = @Description, SYS_UPDATE_DATE = GETDATE() WHERE PKId = @CarId";
        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { CarId = carId, Plate = plate, Description = description }, cancellationToken: cancellationToken));
    }

    public async Task DeleteCarAsync(int carId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _connection.ExecuteAsync(
                new CommandDefinition("DELETE FROM CAR WHERE PKId = @CarId", new { CarId = carId }, cancellationToken: cancellationToken));
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Não é possível remover frota já utilizada em OS.", ex);
        }
    }

    public async Task<IReadOnlyList<HigienicOrderDto>> GetHigienicOrdersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                o.PKId AS OrderId,
                o.SENT_DATE AS SentDate,
                o.SALES_AGENT AS SalesAgent,
                ISNULL(o.NFES_NO, 0) AS NfeNo,
                c.PKId AS ClientId,
                c.SOCIAL_NAME AS SocialName,
                ci.NAME AS City,
                st.CODE AS State,
                car.DESCRIPTION AS CarDescription,
                car.PLATE AS CarPlate,
                o.CAR_KM AS CarKm,
                o.CAR_PROBLEM AS CarProblem
            FROM [ORDER] o
            JOIN CLIENT c ON c.PKId = o.CLIENT_ID
            JOIN STATE st ON st.PKId = c.ADDRESS_STATE_ID
            JOIN CITY ci ON ci.PKId = c.ADDRESS_CITY_ID
            JOIN CAR car ON car.PKId = o.CAR_ID
            WHERE o.PKId IN (
                SELECT MAX(o2.PKId)
                FROM [ORDER] o2
                JOIN ORDER_DETAILS od ON od.ORDER_ID = o2.PKId
                JOIN PRODUCT p ON od.PRODUCT_ID = p.PKId
                JOIN PRODUCT_GROUP pg ON p.GROUP_ID = pg.PKId
                JOIN PRODUCT_CLASS pc ON pc.PKId = pg.PRODUCT_CLASS_ID
                WHERE pc.PROD_SRV_IND = 'S'
                  AND UPPER(p.NAME) LIKE '%HIGI%'
                  AND o2.STATUS = 'E'
                  AND o2.SENT_DATE < GETDATE() - 180
                  AND o2.SENT_DATE > GETDATE() - 360
                  AND o2.CAR_ID IS NOT NULL
                  AND o2.HIGI_PROC = 'N'
                GROUP BY o2.CAR_ID
            )
            ORDER BY o.SENT_DATE ASC";
        var list = await _connection.QueryAsync<HigienicOrderDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<HigienicOrderByClientDto>> GetHigienicOrdersByClientAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM (
                SELECT
                    o.PKId AS OrderId,
                    o.CAR_ID AS CarId,
                    c.PKId AS ClientId,
                    c.SOCIAL_NAME AS SocialName,
                    car.DESCRIPTION AS CarDescription,
                    car.PLATE AS CarPlate,
                    o.SENT_DATE AS SentDate,
                    DATEADD(DAY, 180, o.SENT_DATE) AS RecommendDate,
                    DATEDIFF(DAY, o.SENT_DATE, GETDATE()) AS DaysBehind,
                    ROW_NUMBER() OVER (PARTITION BY o.CAR_ID ORDER BY o.SENT_DATE DESC) AS Seq
                FROM [ORDER] o
                JOIN CLIENT c ON c.PKId = o.CLIENT_ID
                JOIN CAR car ON car.PKId = o.CAR_ID
                WHERE o.CAR_ID IS NOT NULL
                  AND o.SENT_DATE IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM [ORDER] oi2
                      WHERE oi2.CAR_ID = o.CAR_ID
                        AND oi2.SENT_DATE > GETDATE() - 180
                  )
            ) t1
            WHERE t1.Seq = 1
            ORDER BY t1.SocialName, t1.CarId, t1.SentDate";
        var list = await _connection.QueryAsync<HigienicOrderByClientDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task MarkHigienicProcessedAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await _connection.ExecuteAsync(
            new CommandDefinition("UPDATE [ORDER] SET HIGI_PROC = 'Y' WHERE PKId = @OrderId", new { OrderId = orderId }, cancellationToken: cancellationToken));
    }
}
