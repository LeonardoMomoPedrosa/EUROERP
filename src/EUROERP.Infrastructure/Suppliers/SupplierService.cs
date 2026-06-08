using System.Data;
using Dapper;
using EUROERP.Application.Address;
using EUROERP.Application.Suppliers;

namespace EUROERP.Infrastructure.Suppliers;

public class SupplierService : ISupplierService
{
    private readonly IDbConnection _connection;
    private readonly ICityResolutionService _cityResolution;

    public SupplierService(IDbConnection connection, ICityResolutionService cityResolution)
    {
        _connection = connection;
        _cityResolution = cityResolution;
    }

    public async Task<IReadOnlyList<SupplierSummaryDto>> GetListAsync(SupplierFilterDto filter, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT TOP 15
                su.PKId AS Id,
                su.SOCIAL_NAME AS SocialName,
                c.NAME AS City
            FROM SUPPLIER su
            JOIN STATE st ON su.ADDRESS_STATE_ID = st.PKId
            JOIN CITY c ON su.ADDRESS_CITY_ID = c.PKId
            JOIN SUPPLIER_GROUP sg ON su.SUPPLIER_GROUP_ID = sg.PKId
            WHERE su.ACTIVE = 'Y'
            AND (sg.HIDDEN IS NULL OR sg.HIDDEN = '')";

        if (filter.GroupId > 0)
            sql += " AND su.SUPPLIER_GROUP_ID = @GroupId";

        if (!string.IsNullOrWhiteSpace(filter.Name))
            sql += " AND su.SOCIAL_NAME LIKE @NameLike";

        sql += " ORDER BY su.SOCIAL_NAME";

        var nameLike = string.IsNullOrWhiteSpace(filter.Name) ? null : $"%{filter.Name.Trim()}%";

        var list = await _connection.QueryAsync<SupplierSummaryDto>(
            new CommandDefinition(sql, new { filter.GroupId, NameLike = nameLike }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<SupplierSummaryDto>> GetSuggestionsAsync(string term, int groupId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var t = (term ?? "").Trim();
        if (string.IsNullOrEmpty(t))
            return new List<SupplierSummaryDto>();

        var nameLike = $"%{t}%";
        var sql = @"
            SELECT TOP (@Limit)
                su.PKId AS Id,
                su.SOCIAL_NAME AS SocialName,
                c.NAME AS City
            FROM SUPPLIER su
            JOIN STATE st ON su.ADDRESS_STATE_ID = st.PKId
            JOIN CITY c ON su.ADDRESS_CITY_ID = c.PKId
            JOIN SUPPLIER_GROUP sg ON su.SUPPLIER_GROUP_ID = sg.PKId
            WHERE su.ACTIVE = 'Y'
            AND (sg.HIDDEN IS NULL OR sg.HIDDEN = '')
            AND su.SOCIAL_NAME LIKE @NameLike
            AND (@GroupId = 0 OR su.SUPPLIER_GROUP_ID = @GroupId)
            ORDER BY su.SOCIAL_NAME";
        var list = await _connection.QueryAsync<SupplierSummaryDto>(
            new CommandDefinition(sql, new { NameLike = nameLike, GroupId = groupId, Limit = limit }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<SupplierEditDto?> GetByIdAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        const string sqlSupplier = @"
            SELECT
                su.PKId AS Id,
                su.SUPPLIER_GROUP_ID AS SupplierGroupId,
                su.SOCIAL_NAME AS SocialName,
                su.CNPJ AS Cnpj,
                su.STATE_INSCR AS StateInscr,
                su.CONTACT AS Contact,
                su.ADDRESS_STREET AS AddressStreet,
                su.ADDRESS_NUMBER AS AddressNumber,
                su.ADDRESS_COMPLEMENT AS AddressComplement,
                su.ADDRESS_BLOCK AS AddressBlock,
                su.ADDRESS_ZIPCODE AS AddressZipCode,
                su.ADDRESS_STATE_ID AS AddressStateId,
                su.ADDRESS_CITY_ID AS AddressCityId,
                c.NAME AS AddressCityName,
                su.PHONE1 AS Phone1,
                su.PHONE2 AS Phone2,
                su.PHONE3 AS Phone3,
                su.CELULAR AS Celular,
                su.EMAIL AS Email,
                su.BANK_INFO_BANK_ID AS BankInfoBankId,
                su.BANK_INFO_AGENCY AS BankInfoAgency,
                su.BANK_INFO_ACC_NO AS BankInfoAccNo,
                su.BANK_INFO_NAME AS BankInfoName,
                su.SWFFIT_CODE AS SwffitCode,
                su.PAYMENT_METHOD_ID AS PaymentMethodId,
                su.PAYTERM AS PayTerm,
                su.PAYMENT_PLAN AS PaymentPlan,
                su.DISCOUNT AS Discount,
                su.COST_TRANSPORT AS CostTransport,
                su.STOCK_DAYS AS StockDays,
                su.OBS AS Obs
            FROM SUPPLIER su
            LEFT JOIN CITY c ON su.ADDRESS_CITY_ID = c.PKId
            WHERE su.PKId = @SupplierId AND su.ACTIVE = 'Y'";

        var supplier = await _connection.QuerySingleOrDefaultAsync<SupplierEditDto>(
            new CommandDefinition(sqlSupplier, new { SupplierId = supplierId }, cancellationToken: cancellationToken));
        if (supplier == null)
            return null;

        const string sqlDelivery = "SELECT DELIVERY_SUPPLIER_ID AS Id FROM DELIVERY_SUPPLIER_LINK WHERE SUPPLIER_ID = @SupplierId";
        var deliveryIds = await _connection.QueryAsync<int>(
            new CommandDefinition(sqlDelivery, new { SupplierId = supplierId }, cancellationToken: cancellationToken));
        supplier.DeliverySupplierIds = deliveryIds.ToList();

        return supplier;
    }

    public async Task<int> CreateAsync(SupplierCreateDto dto, string applicationId, string userId, CancellationToken cancellationToken = default)
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
            INSERT INTO SUPPLIER (
                SOCIAL_NAME, CNPJ, ADDRESS_STREET, ADDRESS_BLOCK, ADDRESS_NUMBER, ADDRESS_COMPLEMENT,
                PHONE1, PHONE2, PHONE3, ADDRESS_ZIPCODE, ADDRESS_STATE_ID, ADDRESS_CITY_ID,
                BANK_INFO_BANK_ID, BANK_INFO_ACC_NO, BANK_INFO_AGENCY, PAYMENT_METHOD_ID, STOCK_DAYS,
                PAYTERM, PAYMENT_PLAN, SYS_CREATION_DATE, APPLICATION_ID, USER_ID,
                FAX_NO, CELULAR, STATE_INSCR, OBS, CONTACT, DISCOUNT, BANK_INFO_NAME, SWFFIT_CODE,
                EMAIL, SUPPLIER_GROUP_ID, COST_TRANSPORT)
            VALUES (
                @SocialName, @Cnpj, @AddressStreet, @AddressBlock, @AddressNumber, @AddressComplement,
                @Phone1, @Phone2, @Phone3, @AddressZipCode, @AddressStateId, @AddressCityId,
                @BankInfoBankId, @BankInfoAccNo, @BankInfoAgency, @PaymentMethodId, @StockDays,
                @PayTerm, @PaymentPlan, GETDATE(), @ApplicationId, @UserId,
                @FaxNo, @Celular, @StateInscr, @Obs, @Contact, @Discount, @BankInfoName, @SwffitCode,
                @Email, @SupplierGroupId, @CostTransport);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        var newId = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlInsert, new
            {
                dto.SocialName,
                Cnpj = dto.Cnpj ?? (object)DBNull.Value,
                AddressStreet = dto.AddressStreet ?? (object)DBNull.Value,
                AddressBlock = dto.AddressBlock ?? (object)DBNull.Value,
                AddressNumber = dto.AddressNumber ?? (object)DBNull.Value,
                AddressComplement = dto.AddressComplement ?? (object)DBNull.Value,
                Phone1 = dto.Phone1 ?? (object)DBNull.Value,
                Phone2 = dto.Phone2 ?? (object)DBNull.Value,
                Phone3 = dto.Phone3 ?? (object)DBNull.Value,
                AddressZipCode = dto.AddressZipCode ?? (object)DBNull.Value,
                dto.AddressStateId,
                addressCityId,
                BankInfoBankId = dto.BankInfoBankId ?? (object)DBNull.Value,
                BankInfoAccNo = dto.BankInfoAccNo ?? (object)DBNull.Value,
                BankInfoAgency = dto.BankInfoAgency ?? (object)DBNull.Value,
                PaymentMethodId = dto.PaymentMethodId ?? (object)DBNull.Value,
                StockDays = dto.StockDays ?? (object)DBNull.Value,
                PayTerm = dto.PayTerm ?? (object)DBNull.Value,
                PaymentPlan = string.IsNullOrEmpty(dto.PaymentPlan) ? "0" : dto.PaymentPlan,
                ApplicationId = appId,
                UserId = usrId,
                FaxNo = (object)DBNull.Value,
                Celular = dto.Celular ?? (object)DBNull.Value,
                StateInscr = dto.StateInscr ?? (object)DBNull.Value,
                Obs = dto.Obs ?? (object)DBNull.Value,
                Contact = dto.Contact ?? (object)DBNull.Value,
                Discount = dto.Discount ?? (object)DBNull.Value,
                BankInfoName = dto.BankInfoName ?? (object)DBNull.Value,
                SwffitCode = dto.SwffitCode ?? (object)DBNull.Value,
                Email = dto.Email ?? (object)DBNull.Value,
                dto.SupplierGroupId,
                CostTransport = dto.CostTransport ?? (object)DBNull.Value
            }, cancellationToken: cancellationToken));

        const string sqlLink = "INSERT INTO DELIVERY_SUPPLIER_LINK (SUPPLIER_ID, DELIVERY_SUPPLIER_ID) VALUES (@SupplierId, @DeliverySupplierId)";
        foreach (var delId in dto.DeliverySupplierIds)
        {
            if (delId > 0)
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlLink, new { SupplierId = newId, DeliverySupplierId = delId }, cancellationToken: cancellationToken));
            }
        }

        return newId;
    }

    public async Task<bool> UpdateAsync(SupplierEditDto dto, CancellationToken cancellationToken = default)
    {
        var addressCityId = dto.AddressCityId;
        if (!string.IsNullOrWhiteSpace(dto.AddressCityName) && dto.AddressStateId > 0)
        {
            addressCityId = await _cityResolution.ResolveOrCreateCityAsync(
                dto.AddressStateId, dto.AddressCityName, dto.AddressCityIbge, null, cancellationToken);
        }

        const string sqlUpdate = @"
            UPDATE SUPPLIER SET
                SUPPLIER_GROUP_ID = @SupplierGroupId,
                SOCIAL_NAME = @SocialName,
                CNPJ = @Cnpj,
                STATE_INSCR = @StateInscr,
                CONTACT = @Contact,
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
                CELULAR = @Celular,
                EMAIL = @Email,
                BANK_INFO_BANK_ID = @BankInfoBankId,
                BANK_INFO_AGENCY = @BankInfoAgency,
                BANK_INFO_ACC_NO = @BankInfoAccNo,
                BANK_INFO_NAME = @BankInfoName,
                SWFFIT_CODE = @SwffitCode,
                PAYMENT_METHOD_ID = @PaymentMethodId,
                PAYTERM = @PayTerm,
                PAYMENT_PLAN = @PaymentPlan,
                DISCOUNT = @Discount,
                COST_TRANSPORT = @CostTransport,
                STOCK_DAYS = @StockDays,
                OBS = @Obs,
                SYS_UPDATE_DATE = GETDATE()
            WHERE PKId = @Id";

        var rows = await _connection.ExecuteAsync(
            new CommandDefinition(sqlUpdate, new
            {
                dto.Id,
                dto.SupplierGroupId,
                dto.SocialName,
                Cnpj = dto.Cnpj ?? (object)DBNull.Value,
                StateInscr = dto.StateInscr ?? (object)DBNull.Value,
                Contact = dto.Contact ?? (object)DBNull.Value,
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
                Celular = dto.Celular ?? (object)DBNull.Value,
                Email = dto.Email ?? (object)DBNull.Value,
                BankInfoBankId = dto.BankInfoBankId ?? (object)DBNull.Value,
                BankInfoAgency = dto.BankInfoAgency ?? (object)DBNull.Value,
                BankInfoAccNo = dto.BankInfoAccNo ?? (object)DBNull.Value,
                BankInfoName = dto.BankInfoName ?? (object)DBNull.Value,
                SwffitCode = dto.SwffitCode ?? (object)DBNull.Value,
                PaymentMethodId = dto.PaymentMethodId ?? (object)DBNull.Value,
                PayTerm = dto.PayTerm ?? (object)DBNull.Value,
                PaymentPlan = string.IsNullOrEmpty(dto.PaymentPlan) ? "0" : dto.PaymentPlan,
                Discount = dto.Discount ?? (object)DBNull.Value,
                CostTransport = dto.CostTransport ?? (object)DBNull.Value,
                StockDays = dto.StockDays ?? (object)DBNull.Value,
                Obs = dto.Obs ?? (object)DBNull.Value
            }, cancellationToken: cancellationToken));

        if (rows == 0)
            return false;

        await _connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM DELIVERY_SUPPLIER_LINK WHERE SUPPLIER_ID = @SupplierId", new { SupplierId = dto.Id }, cancellationToken: cancellationToken));

        const string sqlLink = "INSERT INTO DELIVERY_SUPPLIER_LINK (SUPPLIER_ID, DELIVERY_SUPPLIER_ID) VALUES (@SupplierId, @DeliverySupplierId)";
        foreach (var delId in dto.DeliverySupplierIds)
        {
            if (delId > 0)
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlLink, new { SupplierId = dto.Id, DeliverySupplierId = delId }, cancellationToken: cancellationToken));
            }
        }

        return true;
    }

    public async Task<IReadOnlyList<SupplierMassItemDto>> GetListByGroupIdsAsync(IEnumerable<int> groupIds, CancellationToken cancellationToken = default)
    {
        var ids = groupIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0)
            return new List<SupplierMassItemDto>();

        const string sql = @"
            SELECT TOP 100
                su.PKId AS Id,
                su.SOCIAL_NAME AS SocialName,
                su.SUPPLIER_GROUP_ID AS SupplierGroupId,
                su.PAYMENT_METHOD_ID AS PaymentMethodId,
                su.PAYTERM AS Payterm,
                su.PAYMENT_PLAN AS PaymentPlan,
                su.STOCK_DAYS AS StockDays,
                su.GNRL_ORDERING AS GnrlOrdering,
                su.OPERATION_PERC AS OperationPerc,
                su.ADMIN_PERC AS AdminPerc,
                su.SALES_PERC AS SalesPerc
            FROM SUPPLIER su
            JOIN SUPPLIER_GROUP sg ON su.SUPPLIER_GROUP_ID = sg.PKId
            WHERE su.ACTIVE = 'Y'
            AND (sg.HIDDEN IS NULL OR sg.HIDDEN = '')
            AND su.SUPPLIER_GROUP_ID IN @GroupIds
            ORDER BY su.SUPPLIER_GROUP_ID, su.SOCIAL_NAME";

        var list = await _connection.QueryAsync<SupplierMassItemDto>(
            new CommandDefinition(sql, new { GroupIds = ids }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<(bool Success, string Message)> UpdateMassRowAsync(SupplierMassUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Id <= 0)
            return (false, "Id inválido.");

        var paymentPlan = string.IsNullOrWhiteSpace(dto.PaymentPlan) ? "0" : dto.PaymentPlan.Trim();
        if (!ValidatePaymentPlan(paymentPlan))
            return (false, "Plano de pagamento deve ser inteiros separados por vírgula em ordem crescente (ex.: 30,60,90).");

        if (dto.Payterm is < 0 or > 300)
            return (false, "Prazo (dias) deve estar entre 0 e 300.");
        if (dto.StockDays is < 0 or > 300)
            return (false, "Dias estoque deve estar entre 0 e 300.");
        if (dto.OperationPerc is < 0 or > 100)
            return (false, "% C.Oper. deve estar entre 0 e 100.");
        if (dto.AdminPerc is < 0 or > 100)
            return (false, "% D.Adm. deve estar entre 0 e 100.");
        if (dto.SalesPerc is < 0 or > 100)
            return (false, "% D.Vendas deve estar entre 0 e 100.");

        const string sql = @"
            UPDATE SUPPLIER SET
                SUPPLIER_GROUP_ID = @SupplierGroupId,
                PAYMENT_METHOD_ID = @PaymentMethodId,
                PAYTERM = @Payterm,
                PAYMENT_PLAN = @PaymentPlan,
                STOCK_DAYS = @StockDays,
                GNRL_ORDERING = @GnrlOrdering,
                OPERATION_PERC = @OperationPerc,
                ADMIN_PERC = @AdminPerc,
                SALES_PERC = @SalesPerc,
                SYS_UPDATE_DATE = GETDATE()
            WHERE PKId = @Id";

        try
        {
            var rows = await _connection.ExecuteAsync(
                new CommandDefinition(sql, new
                {
                    dto.Id,
                    dto.SupplierGroupId,
                    PaymentMethodId = (dto.PaymentMethodId is null || dto.PaymentMethodId == 0) ? (object)DBNull.Value : dto.PaymentMethodId,
                    Payterm = (byte)Math.Clamp(dto.Payterm ?? 0, 0, 255),
                    PaymentPlan = paymentPlan,
                    StockDays = dto.StockDays.HasValue ? (byte?)Math.Clamp(dto.StockDays.Value, 0, 255) : null,
                    dto.GnrlOrdering,
                    OperationPerc = dto.OperationPerc.HasValue ? (byte?)dto.OperationPerc.Value : null,
                    AdminPerc = dto.AdminPerc.HasValue ? (byte?)dto.AdminPerc.Value : null,
                    SalesPerc = dto.SalesPerc.HasValue ? (byte?)dto.SalesPerc.Value : null
                }, cancellationToken: cancellationToken));

            return rows > 0 ? (true, "Atualizado") : (false, "Fornecedor não encontrado.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static bool ValidatePaymentPlan(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "0")
            return true;
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var prev = -1;
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var num))
                return false;
            if (num <= prev)
                return false;
            prev = num;
        }
        return true;
    }
}
