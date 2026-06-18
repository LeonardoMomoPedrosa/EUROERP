using System.Data;
using Dapper;
using EUROERP.Application.Orders;
using Microsoft.Extensions.Configuration;

namespace EUROERP.Infrastructure.Orders;

public class OrderService : IOrderService
{
    private const string RoleComissao = "COMISSAO";
    private readonly IDbConnection _connection;
    private readonly string _applicationName;
    private readonly int _animalTaxLionProductId;

    public OrderService(IDbConnection connection, IConfiguration configuration)
    {
        _connection = connection;
        _applicationName = configuration["Authentication:ApplicationName"] ?? "LionSystem";
        var animalTaxStr = configuration["Ecom:AnimalTaxLionProductId"];
        _animalTaxLionProductId = int.TryParse(animalTaxStr, out var at) ? at : 0;
    }

    /// <summary>E-commerce animal fee SKU: never adjust ERP stock (import adds the line without deducting).</summary>
    private bool SkipStockForAnimalTaxProduct(int productId) =>
        _animalTaxLionProductId > 0 && productId == _animalTaxLionProductId;

    public async Task<IReadOnlyList<SalesAgentDto>> GetSalesAgentsAsync(CancellationToken cancellationToken = default)
    {
        var appId = await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false);
        if (appId == null)
            return new List<SalesAgentDto>();

        const string sql = @"
            SELECT u.UserName
            FROM aspnet_Users u
            INNER JOIN aspnet_UsersInRoles ur ON u.UserId = ur.UserId
            INNER JOIN aspnet_Roles r ON r.RoleId = ur.RoleId
            WHERE r.ApplicationId = @ApplicationId AND r.LoweredRoleName = LOWER(@RoleName)
            ORDER BY u.UserName";
        var list = await _connection.QueryAsync<SalesAgentDto>(
            new CommandDefinition(sql, new { ApplicationId = appId, RoleName = RoleComissao }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<IReadOnlyList<LastOrderDto>> GetLastOrdersBySalesAgentAsync(string salesAgentUserName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(salesAgentUserName))
            return new List<LastOrderDto>();

        const string sql = @"
            SELECT TOP 10
                o.PKId AS OrderId,
                c.FANTASY_NAME AS FantasyName,
                o.STATUS AS Status,
                o.MODE AS Mode,
                om.DESCRIPTION AS ModeDescription
            FROM [ORDER] o
            INNER JOIN ORDER_MODE om ON o.MODE = om.ORDER_MODE
            INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            WHERE o.SALES_AGENT = @SalesAgent
            ORDER BY o.PKId DESC";
        var list = await _connection.QueryAsync<LastOrderDto>(
            new CommandDefinition(sql, new { SalesAgent = salesAgentUserName.Trim() }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<bool> IsClientDelinquentAsync(int clientId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP 1
                ISNULL(c.ALLOW_DELINQ, GETDATE() - 2) AS AllowDelinq,
                c.IGNORE_DELINQ AS IgnoreDelinq
            FROM FINANCE_BTR btr
            JOIN FINANCE_BTR_DETAIL btrd ON btr.PKId = btrd.FINANCE_BTR_ID AND btrd.AMOUNT > 0
            JOIN [ORDER] o ON o.BTR_ID = btr.PKId
            JOIN CLIENT c ON btr.CLIENT_ID = c.PKId
            WHERE btr.CLIENT_ID = @ClientId
              AND btrd.STATUS = 'U'
              AND CAST(btrd.DUE_DATE AS DATE) <= CAST(DATEADD(day, -1, GETDATE()) AS DATE)
            ORDER BY btrd.DUE_DATE ASC";

        var row = await _connection.QuerySingleOrDefaultAsync<DelinqRow>(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row == null || row.IgnoreDelinq)
            return false;

        var allowanceHours = DateTime.Today.DayOfWeek == DayOfWeek.Monday ? 72.0 : 24.0;
        return (DateTime.Today - row.AllowDelinq).TotalHours > allowanceHours;
    }

    public async Task<int> CreateOrderAsync(CreateOrderDto dto, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var salesAgent = !string.IsNullOrWhiteSpace(dto.SalesAgent) ? dto.SalesAgent.Trim() : userId;
        if (salesAgent.Length > 20) salesAgent = salesAgent[..20];
        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;
        var mode = dto.Mode is "Q" or "q" ? "Q" : "S";
        var orderType = string.IsNullOrEmpty(dto.OrderType) ? "P" : dto.OrderType[..1];

        const string sqlOrder = @"
            INSERT INTO [ORDER] (
                SYS_CREATION_DATE, APPLICATION_ID, USER_ID, LAST_ACTV, CLIENT_ID, STATUS, MODE,
                SHIPMENT_COST, STATUS_CHG_DATE, BTR_ID, SALES_AGENT, ORDER_TYPE
            ) VALUES (
                GETDATE(), @ApplicationId, @UserId, 'NEW', @ClientId, 'I', @Mode,
                0, GETDATE(), NULL, @SalesAgent, @OrderType
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var orderId = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlOrder, new
            {
                ApplicationId = appId,
                UserId = uid,
                dto.ClientId,
                Mode = mode,
                SalesAgent = salesAgent,
                OrderType = orderType
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        const string sqlHist = @"
            INSERT INTO [ORDER_STATUS_HIST] (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, NEW_STATUS, ORDER_ID)
            VALUES (GETDATE(), @UserId, @ApplicationId, 'I', @OrderId)";
        await _connection.ExecuteAsync(
            new CommandDefinition(sqlHist, new { UserId = uid, ApplicationId = appId, OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return orderId;
    }

    public async Task<OrderHeaderDto?> GetOrderHeaderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                o.PKId AS OrderId,
                o.ORDER_TYPE AS OrderType,
                o.MODE AS Mode,
                om.DESCRIPTION AS ModeDescription,
                o.SYS_CREATION_DATE AS OrderDate,
                o.STATUS AS Status,
                o.SENT_DATE AS SentDate,
                c.PKId AS ClientId,
                c.FANTASY_NAME AS ClientName,
                c.SOCIAL_NAME AS SocialName,
                ISNULL(o.SALES_AGENT, '') AS SalesAgent,
                c.ADDRESS_STREET AS AddressStreet,
                ISNULL(c.ADDRESS_BLOCK, '') AS AddressBlock,
                ISNULL(c.ADDRESS_ZIPCODE, '') AS AddressZipCode,
                ISNULL(c.ADDRESS_NUMBER, '') AS AddressNumber,
                ISNULL(c.ADDRESS_COMPLEMENT, '') AS AddressComplement,
                ci.NAME AS City,
                st.CODE AS State,
                ISNULL(c.PHONE1, '') AS Phone1,
                ISNULL(c.PHONE2, '') AS Phone2,
                ISNULL(c.PHONE3, '') AS Phone3,
                ISNULL(c.CELULAR, '') AS Celular,
                ISNULL(o.CREDIT, 0) AS Credit,
                o.DISCOUNT AS Discount,
                ISNULL(o.OTHER_EXPENSES, 0) AS OtherExpenses,
                ISNULL(o.SHIPMENT_COST, 0) AS ShipmentCost,
                ISNULL(o.CHARGE_SHIPMENT, 1) AS ChargeShipment,
                o.SITE_ORDER_ID AS SiteOrderId,
                o.ML_ORDER_ID AS MlOrderId,
                o.CAR_ID AS CarId,
                car.DESCRIPTION AS CarDescription,
                car.PLATE AS CarPlate,
                o.CAR_KM AS CarKm,
                o.CAR_PROBLEM AS CarProblem
            FROM [ORDER] o
            JOIN ORDER_MODE om ON o.MODE = om.ORDER_MODE
            JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            JOIN [CITY] ci ON ci.PKId = c.ADDRESS_CITY_ID
            JOIN [STATE] st ON c.ADDRESS_STATE_ID = st.PKId
            LEFT JOIN [CAR] car ON car.PKId = o.CAR_ID
            WHERE o.PKId = @OrderId";
        var row = await _connection.QueryFirstOrDefaultAsync<OrderHeaderDto>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null)
            return null;

        var (limit, due) = await GetClientDueAmountAsync(row.ClientId, cancellationToken).ConfigureAwait(false);
        row.LimitAmount = limit;
        row.DueAmount = due;
        row.OrderTotalAmount = await GetOrderTotalAmountAsync(orderId, cancellationToken).ConfigureAwait(false);
        row.BalanceForPurchase = row.LimitAmount - row.DueAmount - row.OrderTotalAmount;
        return row;
    }

    public async Task<OrderByIdDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                o.SYS_CREATION_DATE AS OrderDate,
                o.SENT_DATE AS SentDate,
                o.STATUS AS Status,
                ISNULL(o.NFE_KEY, '') AS NfeKey
            FROM [ORDER] o
            WHERE o.PKId = @OrderId";
        return await _connection.QueryFirstOrDefaultAsync<OrderByIdDto>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrderDetailItemDto>> GetOrderDetailsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                od.PRODUCT_ID AS ProductId,
                od.BOX AS Box,
                CAST(pg.PRODUCT_CLASS_ID AS VARCHAR) + SUBSTRING('000000' + CAST(od.PRODUCT_ID AS VARCHAR), LEN(CAST(od.PRODUCT_ID AS VARCHAR)), 7) AS LionCode,
                p.NAME AS Name,
                od.QUANTITY AS Quantity,
                CAST(ROUND(od.PRICE, 2) AS DECIMAL(14,2)) AS Price,
                od.DISCOUNT AS Discount,
                CAST(ROUND(ROUND(ROUND(od.PRICE, 2) * (1 - od.DISCOUNT/100), 2) * od.QUANTITY, 2) AS DECIMAL(14,2)) AS TotalPrice
            FROM [ORDER_DETAILS] od
            LEFT JOIN [PRODUCT] p ON p.PKId = od.PRODUCT_ID
            JOIN [PRODUCT_GROUP] pg ON p.GROUP_ID = pg.PKId
            WHERE od.ORDER_ID = @OrderId AND od.QUANTITY > 0
            ORDER BY p.NAME";
        var list = await _connection.QueryAsync<OrderDetailItemDto>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<ProductForSaleDto?> GetProductForSaleAsync(int productId, int orderId, int clientId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                p.PKId AS ProductId,
                p.NAME AS Name,
                p.STOCK AS Stock,
                mp.PRICE AS Price,
                p.COST_FINAL AS CostFinal,
                ISNULL(pg.IGNORE_ORDER_DISC, 0) AS IOD,
                c.SYMBOL AS CurrencySymbol,
                cli.COST_IND AS CostInd,
                p.CURRENCY_ID AS CurrencyId,
                ISNULL(d.DISCOUNT, 0) AS Discount,
                ISNULL(cc.CONVERSION, 1) AS Conversion
            FROM [PRODUCT] p
            JOIN [PRODUCT_GROUP] pg ON pg.PKId = p.GROUP_ID
            JOIN [PRODUCT_CLASS] pc ON pc.PKId = pg.PRODUCT_CLASS_ID
            JOIN [ORDER] o ON o.PKId = @OrderId
            JOIN [CLIENT] cli ON cli.PKId = o.CLIENT_ID
            JOIN [MARKET_PRODUCT] mp ON mp.MARKET_ID = cli.MARKET_ID AND mp.PRODUCT_ID = p.PKId
            JOIN [MARKET] mkt ON mkt.PKId = mp.MARKET_ID
            JOIN [CURRENCY] c ON c.PKId = mkt.CURRENCY_ID
            LEFT JOIN [DISCOUNT] d ON d.PRODUCT_GROUP_ID = p.GROUP_ID AND d.CLIENT_ID = @ClientId
            JOIN [CURRENCY_CONVERSION] cc ON cc.SOURCE_CURRENCY_ID = mkt.CURRENCY_ID AND cc.TARGET_CURRENCY_ID = p.CURRENCY_ID
            WHERE p.PKId = @ProductId AND p.ACTIVE = 'Y'";
        var row = await _connection.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(sql, new { ProductId = productId, OrderId = orderId, ClientId = clientId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null)
            return null;
        return new ProductForSaleDto
        {
            ProductId = (int)row.ProductId,
            Name = (string)row.Name,
            Stock = (int)row.Stock,
            Price = (decimal)row.Price,
            Discount = (decimal)row.Discount,
            CurrencySymbol = (string)row.CurrencySymbol,
            Conversion = (decimal)row.Conversion,
            CostInd = (bool)row.CostInd,
            CostFinal = (decimal)row.CostFinal
        };
    }

    public async Task<IReadOnlyList<ProductForSaleSuggestionDto>> GetProductSuggestionsForSaleAsync(string term, int orderId, int limit = 15, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            return new List<ProductForSaleSuggestionDto>();

        var nameLike = $"%{term.Trim()}%";
        const string sql = @"
            SELECT TOP (@Limit)
                p.PKId AS ProductId,
                p.NAME AS Name,
                p.STOCK AS Stock,
                ROUND(mp.PRICE * ISNULL(cc.CONVERSION, 1), 2) AS Price
            FROM [PRODUCT] p
            JOIN [PRODUCT_GROUP] pg ON pg.PKId = p.GROUP_ID
            JOIN [ORDER] o ON o.PKId = @OrderId
            JOIN [CLIENT] cli ON cli.PKId = o.CLIENT_ID
            JOIN [MARKET_PRODUCT] mp ON mp.MARKET_ID = cli.MARKET_ID AND mp.PRODUCT_ID = p.PKId
            JOIN [MARKET] mkt ON mkt.PKId = mp.MARKET_ID
            LEFT JOIN [CURRENCY_CONVERSION] cc ON cc.SOURCE_CURRENCY_ID = mkt.CURRENCY_ID AND cc.TARGET_CURRENCY_ID = p.CURRENCY_ID
            WHERE p.ACTIVE = 'Y' AND p.NAME LIKE @NameLike
            ORDER BY p.NAME";
        var list = await _connection.QueryAsync<ProductForSaleSuggestionDto>(
            new CommandDefinition(sql, new { OrderId = orderId, NameLike = nameLike, Limit = limit }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task AddOrderDetailAsync(int orderId, int productId, decimal quantity, int clientId, string applicationId, string userId, decimal? unitPriceOverride = null, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Informe quantidade maior que zero.");

        var product = await GetProductForSaleAsync(productId, orderId, clientId, cancellationToken).ConfigureAwait(false);
        if (product == null)
            throw new InvalidOperationException("Produto inexistente ou inativo para este cliente.");
        if (product.Stock < quantity)
            throw new InvalidOperationException("Estoque insuficiente.");

        var details = await GetOrderDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        var existing = details.FirstOrDefault(d => d.ProductId == productId);
        var box = (byte)0;
        decimal price = unitPriceOverride is { } op && op > 0
            ? decimal.Round(op, 2)
            : (product.CostInd ? product.CostFinal : product.Price);

        if (existing != null)
        {
            var newQty = existing.Quantity + quantity;
            if (!SkipStockForAnimalTaxProduct(productId))
                await OperateStockAsync(productId, orderId, clientId, -quantity, "Adicionou ao pedido " + orderId, userId, cancellationToken).ConfigureAwait(false);
            const string upd = @"
                UPDATE [ORDER_DETAILS] SET QTD_ORDERED = @QtdOrdered, QUANTITY = @Quantity
                WHERE ORDER_ID = @OrderId AND PRODUCT_ID = @ProductId AND BOX = @Box";
            await _connection.ExecuteAsync(new CommandDefinition(upd, new { QtdOrdered = newQty, Quantity = newQty, OrderId = orderId, ProductId = productId, Box = box }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return;
        }

        if (!SkipStockForAnimalTaxProduct(productId))
            await OperateStockAsync(productId, orderId, clientId, -quantity, "Adicionou ao pedido " + orderId, userId, cancellationToken).ConfigureAwait(false);
        const string ins = @"
            INSERT INTO [ORDER_DETAILS] (ORDER_ID, BOX, EXTERNAL_PID, PRODUCT_ID, QTD_ORDERED, QUANTITY, PRICE, COST_FINAL, CURRENCY_ID, CONVERSION, IGNORE_ORDER_DISC, DISCOUNT)
            VALUES (@OrderId, @Box, NULL, @ProductId, @Quantity, @Quantity, @Price, (SELECT COST_FINAL FROM PRODUCT WHERE PKId = @ProductId), @CurrencyId, @Conversion, @IgnoreOrderDisc, @Discount)";
        var currencyId = await GetProductCurrencyIdAsync(productId, cancellationToken).ConfigureAwait(false);
        await _connection.ExecuteAsync(new CommandDefinition(ins, new
        {
            OrderId = orderId,
            Box = box,
            ProductId = productId,
            Quantity = quantity,
            Price = price,
            CurrencyId = currencyId,
            Conversion = product.Conversion,
            IgnoreOrderDisc = product.CostInd,
            Discount = product.Discount
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task AddOrderDetailFromSiteAsync(int orderId, int productId, int quantity, decimal price, int clientId, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        const byte box = 0;
        var currencyId = await GetProductCurrencyIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (!SkipStockForAnimalTaxProduct(productId))
            await OperateStockAsync(productId, orderId, clientId, -quantity, "Importação Site", userId, cancellationToken).ConfigureAwait(false);
        const string ins = @"
            INSERT INTO [ORDER_DETAILS] (ORDER_ID, BOX, EXTERNAL_PID, PRODUCT_ID, QTD_ORDERED, QUANTITY, PRICE, COST_FINAL, CURRENCY_ID, CONVERSION, IGNORE_ORDER_DISC, DISCOUNT)
            VALUES (@OrderId, @Box, NULL, @ProductId, @Quantity, @Quantity, @Price, (SELECT COST_FINAL FROM PRODUCT WHERE PKId = @ProductId), @CurrencyId, 1, 0, 0)";
        await _connection.ExecuteAsync(new CommandDefinition(ins, new
        {
            OrderId = orderId,
            Box = box,
            ProductId = productId,
            Quantity = quantity,
            Price = price,
            CurrencyId = currencyId
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task RemoveOrderDetailAsync(int orderId, int productId, int clientId, CancellationToken cancellationToken = default)
    {
        const string getQty = "SELECT QUANTITY FROM [ORDER_DETAILS] WHERE ORDER_ID = @OrderId AND PRODUCT_ID = @ProductId AND BOX = 0";
        var qty = await _connection.ExecuteScalarAsync<decimal?>(new CommandDefinition(getQty, new { OrderId = orderId, ProductId = productId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (qty == null || qty == 0)
            return;
        if (!SkipStockForAnimalTaxProduct(productId))
            await OperateStockAsync(productId, orderId, clientId, qty.Value, "Removeu produto do pedido " + orderId, null!, cancellationToken).ConfigureAwait(false);
        const string del = "DELETE FROM [ORDER_DETAILS] WHERE ORDER_ID = @OrderId AND PRODUCT_ID = @ProductId AND BOX = 0";
        await _connection.ExecuteAsync(new CommandDefinition(del, new { OrderId = orderId, ProductId = productId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task ResetOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var header = await GetOrderHeaderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (header == null)
            throw new InvalidOperationException("Pedido não encontrado.");
        if (header.Status != "I" && header.Status != "R")
            throw new InvalidOperationException("Só é possível zerar pedidos incompletos ou reabertos.");

        var details = await GetOrderDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        foreach (var d in details)
        {
            if (!SkipStockForAnimalTaxProduct(d.ProductId))
                await OperateStockAsync(d.ProductId, orderId, header.ClientId, d.Quantity, "Resetando pedido " + orderId, null!, cancellationToken).ConfigureAwait(false);
        }

        const string sql = "UPDATE [ORDER_DETAILS] SET QTD_ORDERED = 0, QUANTITY = 0 WHERE ORDER_ID = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpdateOrderExtraTaxesAsync(int orderId, decimal? discount, decimal? credit, decimal? otherExpenses, decimal? shipmentCost, bool? chargeShipment = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE [ORDER] SET
                DISCOUNT = @Discount,
                CREDIT = @Credit,
                OTHER_EXPENSES = @OtherExpenses,
                SHIPMENT_COST = @ShipmentCost,
                CHARGE_SHIPMENT = COALESCE(@ChargeShipment, CHARGE_SHIPMENT)
            WHERE PKId = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            OrderId = orderId,
            Discount = discount ?? 0,
            Credit = credit ?? 0,
            OtherExpenses = otherExpenses ?? 0,
            ShipmentCost = shipmentCost ?? 0,
            ChargeShipment = chargeShipment
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpdateOrderCarKmAndProblemAsync(int orderId, decimal? carKm, string? carProblem, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE [ORDER] SET CAR_KM = @CarKm, CAR_PROBLEM = @CarProblem WHERE PKId = @OrderId;
            UPDATE CAR SET LAST_KM = @CarKm WHERE PKId = (SELECT CAR_ID FROM [ORDER] WHERE PKId = @OrderId AND CAR_ID IS NOT NULL)";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            OrderId = orderId,
            CarKm = carKm,
            CarProblem = carProblem?.Length > 500 ? carProblem[..500] : carProblem
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpdateOrderCfeAsync(int orderId, string cfeProtocol, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;
        var normalizedProtocol = (cfeProtocol ?? string.Empty).Trim();
        if (normalizedProtocol.Length > 200)
            normalizedProtocol = normalizedProtocol[..200];

        const string sql = @"
            UPDATE [ORDER]
            SET CFE_PROTOCOL = @CfeProtocol,
                SYS_UPDATE_DATE = GETDATE(),
                APPLICATION_ID = @ApplicationId,
                USER_ID = @UserId
            WHERE PKId = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            OrderId = orderId,
            CfeProtocol = normalizedProtocol,
            ApplicationId = appId,
            UserId = uid
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task CancelOrderAsync(int orderId, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var header = await GetOrderHeaderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (header == null)
            throw new InvalidOperationException("Pedido não encontrado.");
        if (header.Status != "I" && header.Status != "R")
            throw new InvalidOperationException("Só é possível cancelar pedidos incompletos ou reabertos.");

        var details = await GetOrderDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        foreach (var d in details)
        {
            if (!SkipStockForAnimalTaxProduct(d.ProductId))
                await OperateStockAsync(d.ProductId, orderId, header.ClientId, d.Quantity, "Cancelando pedido " + orderId, userId, cancellationToken).ConfigureAwait(false);
        }

        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;

        const string updOrder = "UPDATE [ORDER] SET STATUS = 'C', BTR_ID = NULL, ML_ORDER_ID = NULL, LAST_ACTV = 'CANCEL' WHERE PKId = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(updOrder, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        const string updDetails = "UPDATE [ORDER_DETAILS] SET QTD_ORDERED = 0, QUANTITY = 0 WHERE ORDER_ID = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(updDetails, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        const string insHist = "INSERT INTO [ORDER_STATUS_HIST] (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, NEW_STATUS, ORDER_ID) VALUES (GETDATE(), @UserId, @ApplicationId, 'C', @OrderId)";
        await _connection.ExecuteAsync(new CommandDefinition(insHist, new { UserId = uid, ApplicationId = appId, OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<(decimal Limit, decimal Due)> GetClientDueAmountAsync(int clientId, CancellationToken ct)
    {
        const string sql = @"
            SELECT SUM(ISNULL(t1.A, 0) - ISNULL(t1.R, 0)) AS DUE_AMOUNT, MAX(ISNULL(t1.LIMIT, 0)) AS LIMIT
            FROM (
                SELECT ISNULL(btrd.AMOUNT, 0) AS A, SUM(ISNULL(fr.AMOUNT, 0)) AS R, ISNULL(c.LIMIT_AMOUNT, 0) AS LIMIT
                FROM [CLIENT] c
                LEFT JOIN [ORDER] o ON o.CLIENT_ID = c.PKId AND o.STATUS IN ('E', 'F')
                LEFT JOIN [FINANCE_BTR] btr ON o.BTR_ID = btr.PKId
                LEFT JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
                LEFT JOIN [FINANCE_RECEIVE] fr ON fr.FINANCE_BTR_ID = btr.PKId AND fr.TERM_NO = btrd.TERM_NO
                WHERE c.PKId = @ClientId
                GROUP BY o.PKId, btrd.TERM_NO, btrd.AMOUNT, c.LIMIT_AMOUNT
            ) t1
            GROUP BY t1.LIMIT";
        var row = await _connection.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: ct)).ConfigureAwait(false);
        if (row == null)
        {
            const string fallback = "SELECT ISNULL(LIMIT_AMOUNT, 0) AS LIMIT FROM [CLIENT] WHERE PKId = @ClientId";
            var limit = await _connection.ExecuteScalarAsync<decimal>(new CommandDefinition(fallback, new { ClientId = clientId }, cancellationToken: ct)).ConfigureAwait(false);
            return (limit, 0);
        }
        return ((decimal)row.LIMIT, (decimal)row.DUE_AMOUNT);
    }

    private async Task<decimal> GetOrderTotalAmountAsync(int orderId, CancellationToken ct)
    {
        const string sql = @"
            SELECT (SUM(t1.TOTAL) - MAX(t1.CREDIT)) * (1 - MAX(t1.DISC) / 100) + MAX(t1.OE) + MAX(t1.SHIP) AS CONVERTED_TOTAL_NET_PRICE
            FROM (
                SELECT CAST(SUM(ROUND(ROUND(ROUND(price * conversion, 2) * (1 - od.discount/100), 2) * quantity, 2)) AS DECIMAL(14,2)) AS TOTAL,
                    ISNULL(o.CREDIT, 0) AS CREDIT, ISNULL(o.OTHER_EXPENSES, 0) AS OE,
                    (ISNULL(od.ignore_order_disc, 0) - 1) * -1 * ISNULL(o.DISCOUNT, 0) AS DISC,
                    CASE WHEN ISNULL(o.CHARGE_SHIPMENT, 1) = 1 THEN ISNULL(o.SHIPMENT_COST, 0) ELSE 0 END AS SHIP
                FROM [ORDER_DETAILS] od
                JOIN [ORDER] o ON o.PKId = od.ORDER_ID
                WHERE od.ORDER_ID = @OrderId AND od.QUANTITY > 0
                GROUP BY o.CREDIT, o.DISCOUNT, o.OTHER_EXPENSES, o.SHIPMENT_COST, od.IGNORE_ORDER_DISC
            ) t1";
        var total = await _connection.ExecuteScalarAsync<decimal?>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: ct)).ConfigureAwait(false);
        return total ?? 0;
    }

    private async Task OperateStockAsync(int productId, int orderId, int clientId, decimal amount, string memo, string? userId, CancellationToken ct)
    {
        const string getStock = "SELECT STOCK FROM [PRODUCT] WHERE PKId = @ProductId";
        var stock = await _connection.ExecuteScalarAsync<decimal>(new CommandDefinition(getStock, new { ProductId = productId }, cancellationToken: ct)).ConfigureAwait(false);
        if (amount < 0 && stock < -amount)
            throw new InvalidOperationException("Estoque insuficiente.");
        const string upd = "UPDATE [PRODUCT] SET STOCK = STOCK + @Amount WHERE PKId = @ProductId";
        await _connection.ExecuteAsync(new CommandDefinition(upd, new { ProductId = productId, Amount = amount }, cancellationToken: ct)).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(userId))
        {
            const string insHist = "INSERT INTO [STOCK_HISTORY] (PRODUCT_ID, SYS_CREATION_DATE, USER_ID, PREVIOUS, QUANTITY, MEMO, CLIENT_ID) VALUES (@ProductId, GETDATE(), @UserId, @Previous, @Quantity, @Memo, @ClientId)";
            await _connection.ExecuteAsync(new CommandDefinition(insHist, new { ProductId = productId, UserId = userId.Length > 20 ? userId[..20] : userId, Previous = stock, Quantity = amount, Memo = memo.Length > 100 ? memo[..100] : memo, ClientId = (int?)clientId }, cancellationToken: ct)).ConfigureAwait(false);
        }
    }

    private async Task<byte> GetProductCurrencyIdAsync(int productId, CancellationToken ct)
    {
        const string sql = "SELECT CURRENCY_ID FROM [PRODUCT] WHERE PKId = @ProductId";
        return await _connection.ExecuteScalarAsync<byte>(new CommandDefinition(sql, new { ProductId = productId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private async Task<Guid?> GetApplicationIdAsync(CancellationToken ct)
    {
        const string sql = "SELECT ApplicationId FROM aspnet_Applications WHERE LoweredApplicationName = LOWER(@ApplicationName)";
        return await _connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { ApplicationName = _applicationName }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public Task<OrderPaymentSummaryDto?> GetOrderPaymentSummaryAsync(int orderId, CancellationToken cancellationToken = default) =>
        GetOrderPaymentSummaryCoreAsync(orderId, precomputedTotalToPay: null, cancellationToken);

    public Task<OrderPaymentSummaryDto?> GetOrderPaymentSummaryAsync(int orderId, decimal precomputedTotalToPay, CancellationToken cancellationToken = default) =>
        GetOrderPaymentSummaryCoreAsync(orderId, precomputedTotalToPay, cancellationToken);

    private async Task<OrderPaymentSummaryDto?> GetOrderPaymentSummaryCoreAsync(int orderId, decimal? precomputedTotalToPay, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.PKId AS OrderId, o.CLIENT_ID AS ClientId, ISNULL(o.CREDIT, 0) AS Credit, ISNULL(o.DISCOUNT, 0) AS Discount,
                ISNULL(o.OTHER_EXPENSES, 0) AS OtherExpenses, ISNULL(o.SHIPMENT_COST, 0) AS ShipmentCost,
                o.STATUS AS Status, o.BTR_ID AS BtrId, o.SALES_AGENT AS SalesAgent,
                c.FANTASY_NAME AS ClientName, ISNULL(c.AVG_PAYTERM, 0) AS AvgPayterm,
                mkt.CURRENCY_ID AS CurrencyId, cur.SYMBOL AS CurrencySymbol
            FROM [ORDER] o
            JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            JOIN [MARKET] mkt ON mkt.PKId = c.MARKET_ID
            JOIN [CURRENCY] cur ON cur.PKId = mkt.CURRENCY_ID
            WHERE o.PKId = @OrderId";
        var row = await _connection.QueryFirstOrDefaultAsync<OrderPaymentSummaryDto>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null)
            return null;
        row.TotalToPay = precomputedTotalToPay ?? await GetOrderTotalAmountAsync(orderId, cancellationToken).ConfigureAwait(false);
        return row;
    }

    public async Task<IReadOnlyList<PaymentMethodOptionDto>> GetAllowedPaymentMethodsAsync(int clientId, decimal totalToPay, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT pm.PKId AS Id, pm.NAME AS Name, ISNULL(pm.MAX_TERMS, 0) AS MaxTerms, ISNULL(pm.MIN_AMOUNT, 0) AS MinAmount
            FROM [CLIENT] c
            JOIN [PAYMENT_METHOD] pm ON pm.PKId IN (c.PAYMENT_METHOD_ID, c.PAYMENT_METHOD_ID2, c.PAYMENT_METHOD_ID3)
            WHERE c.PKId = @ClientId AND pm.PKId IS NOT NULL AND LTRIM(RTRIM(ISNULL(pm.NAME,''))) <> ''
            GROUP BY pm.PKId, pm.NAME, pm.MAX_TERMS, pm.MIN_AMOUNT";
        var all = await _connection.QueryAsync<PaymentMethodOptionDto>(new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        var list = new List<PaymentMethodOptionDto>();
        foreach (var pm in all)
        {
            if (pm.Id == 1) continue; // Skip Cheque
            if (totalToPay >= pm.MinAmount || totalToPay == 0)
                list.Add(pm);
        }
        if (list.Count == 0 && totalToPay == 0)
        {
            const string dinheiro = "SELECT PKId AS Id, NAME AS Name, ISNULL(MAX_TERMS, 0) AS MaxTerms, ISNULL(MIN_AMOUNT, 0) AS MinAmount FROM [PAYMENT_METHOD] WHERE PKId = 4";
            var d = await _connection.QueryFirstOrDefaultAsync<PaymentMethodOptionDto>(new CommandDefinition(dinheiro, cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (d != null) list.Add(d);
        }
        return list;
    }

    public async Task FinishOrderWithPaymentAsync(int orderId, byte paymentMethodId, IReadOnlyList<BtrDetailDto> details, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var summary = await GetOrderPaymentSummaryAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (summary == null)
            throw new InvalidOperationException("Pedido não encontrado.");
        if (summary.Status != "I" && summary.Status != "R")
            throw new InvalidOperationException("Só é possível finalizar pedidos incompletos ou reabertos.");
        if (details == null || details.Count == 0)
            throw new InvalidOperationException("Informe ao menos um termo de pagamento.");
        var sumDetails = details.Sum(d => d.Amount);
        if (Math.Abs(sumDetails - summary.TotalToPay) > 0.01m)
            throw new InvalidOperationException($"O total das parcelas deve ser igual ao total a pagar. Total das parcelas: {sumDetails:N2}. Total a pagar: {summary.TotalToPay:N2}.");

        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using var tr = _connection.BeginTransaction();
        try
        {
            if (summary.Status == "R" && summary.BtrId.HasValue && summary.BtrId.Value > 0)
            {
                await _connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE [ORDER] SET BTR_ID = NULL WHERE PKId = @OrderId; DELETE FROM [FINANCE_CHECK] WHERE FINANCE_BTR_ID = @BtrId; DELETE FROM [FINANCE_BTR_DETAIL] WHERE FINANCE_BTR_ID = @BtrId; DELETE FROM [FINANCE_BTR] WHERE PKId = @BtrId",
                    new { OrderId = orderId, BtrId = summary.BtrId.Value },
                    cancellationToken: cancellationToken,
                    transaction: tr)).ConfigureAwait(false);
            }

            const string insBtr = @"
                INSERT INTO [FINANCE_BTR] (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, CLIENT_ID, TERMS, BILL_TYPE, CURRENCY_ID)
                VALUES (GETDATE(), @UserId, @ApplicationId, @ClientId, @Terms, 'O', @CurrencyId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var btrId = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(insBtr, new
            {
                UserId = uid,
                ApplicationId = appId,
                ClientId = summary.ClientId,
                Terms = (byte)details.Count,
                CurrencyId = summary.CurrencyId
            }, cancellationToken: cancellationToken, transaction: tr)).ConfigureAwait(false);

            const string insDetail = @"
                INSERT INTO [FINANCE_BTR_DETAIL] (FINANCE_BTR_ID, TERM_NO, PAYMENT_METHOD_ID, DUE_DATE, AMOUNT, ORIG_AMOUNT, STATUS, MEMO)
                VALUES (@BtrId, @TermNo, @PaymentMethodId, @DueDate, @Amount, @Amount, 'U', @Memo)";
            for (byte i = 0; i < details.Count; i++)
            {
                var d = details[i];
                await _connection.ExecuteAsync(new CommandDefinition(insDetail, new
                {
                    BtrId = btrId,
                    TermNo = (byte)(i + 1),
                    d.PaymentMethodId,
                    d.DueDate,
                    d.Amount,
                    Memo = (d.Memo ?? "").Length > 200 ? (d.Memo ?? "").Substring(0, 200) : (d.Memo ?? "")
                }, cancellationToken: cancellationToken, transaction: tr)).ConfigureAwait(false);
            }

            decimal creditToApply = summary.Credit;
            var totalGross = await _connection.ExecuteScalarAsync<decimal>(new CommandDefinition(
                "SELECT ISNULL(SUM(ROUND(ROUND(ROUND(od.PRICE * od.CONVERSION, 2) * (1 - od.DISCOUNT/100), 2) * od.QUANTITY, 2)), 0) FROM [ORDER_DETAILS] od WHERE od.ORDER_ID = @OrderId AND od.QUANTITY > 0",
                new { OrderId = orderId }, cancellationToken: cancellationToken, transaction: tr)).ConfigureAwait(false);
            if (creditToApply > 0 && creditToApply > totalGross) creditToApply = totalGross;
            if (creditToApply > 0)
            {
                await _connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE [CLIENT] SET CREDIT = ISNULL(CREDIT, 0) + @Amount WHERE PKId = @ClientId",
                    new { Amount = -creditToApply, ClientId = summary.ClientId }, cancellationToken: cancellationToken, transaction: tr)).ConfigureAwait(false);
                const string insCreditHist = "INSERT INTO [CLIENT_CREDIT_HIST] (SYS_CREATION_DATE, APPLICATION_ID, USER_ID, AMOUNT, CLIENT_ID, ORDER_ID, MEMO) VALUES (GETDATE(), @ApplicationId, @UserId, @Amount, @ClientId, @OrderId, @Memo)";
                await _connection.ExecuteAsync(new CommandDefinition(insCreditHist, new { ApplicationId = appId, UserId = uid, Amount = -creditToApply, summary.ClientId, OrderId = orderId, Memo = "" }, cancellationToken: cancellationToken, transaction: tr)).ConfigureAwait(false);
            }

            await _connection.ExecuteAsync(new CommandDefinition(
                "UPDATE [ORDER] SET SYS_UPDATE_DATE = GETDATE(), APPLICATION_ID = @ApplicationId, USER_ID = @UserId, LAST_ACTV = 'FINISHED', STATUS = 'F', CREDIT = @Credit, STATUS_CHG_DATE = GETDATE(), BTR_ID = @BtrId WHERE PKId = @OrderId",
                new { ApplicationId = appId, UserId = uid, Credit = creditToApply, BtrId = btrId, OrderId = orderId },
                cancellationToken: cancellationToken, transaction: tr)).ConfigureAwait(false);

            await _connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO [ORDER_STATUS_HIST] (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, NEW_STATUS, ORDER_ID) VALUES (GETDATE(), @UserId, @ApplicationId, 'F', @OrderId)",
                new { UserId = uid, ApplicationId = appId, OrderId = orderId },
                cancellationToken: cancellationToken, transaction: tr)).ConfigureAwait(false);

            tr.Commit();
        }
        catch
        {
            tr.Rollback();
            throw;
        }
    }

    public async Task ReopenOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sqlOrder = @"
            SELECT o.STATUS AS Status, o.BTR_ID AS BtrId, o.USER_ID AS UserId, o.APPLICATION_ID AS ApplicationId,
                o.NFE_RECEIPT AS NfeReceipt, o.NFE_PROTOCOL_RESULT AS NfeProtocolResult, o.NFE_PROTOCOL AS NfeProtocol
            FROM [ORDER] o WHERE o.PKId = @OrderId";
        var row = await _connection.QueryFirstOrDefaultAsync<dynamic>(new CommandDefinition(sqlOrder, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null)
            throw new InvalidOperationException("Pedido não encontrado.");

        var status = (string)row.Status;
        if (status != "F" && status != "E")
            throw new InvalidOperationException("Impossível reabrir compras que não estejam fechadas.");

        // Igual ao legado: pedido não pode ser reaberto se tiver nota fiscal emitida
        var nfeReceipt = row.NfeReceipt?.ToString()?.Trim() ?? "";
        var nfeProtocolResult = row.NfeProtocolResult?.ToString()?.Trim() ?? "";
        var nfeProtocol = row.NfeProtocol?.ToString()?.Trim() ?? "";
        var hasNfeEmitida = nfeProtocolResult == "100"
            || !string.IsNullOrEmpty(nfeProtocol)
            || nfeReceipt.Length > 1;
        if (hasNfeEmitida)
            throw new InvalidOperationException("Impossível reabrir pedidos que já tenham NFe emitida.");

        var btrIdObj = (object?)row.BtrId;
        var btrId = (btrIdObj != null && btrIdObj != DBNull.Value) ? (int?)Convert.ToInt32(btrIdObj) : null;
        if (btrId.HasValue && btrId.Value > 0)
        {
            var hasPayment = await HasPaymentAsync(btrId.Value, cancellationToken).ConfigureAwait(false);
            if (hasPayment)
                throw new InvalidOperationException("Esta compra já tem baixa de pagamento. Não é possível reabrir.");
        }

        var uid = (string)(row.UserId ?? "SYS");
        uid = uid.Length > 20 ? uid[..20] : uid;
        var appIdStr = (string)(row.ApplicationId ?? "");
        if (string.IsNullOrEmpty(appIdStr) || appIdStr.Length > 8) appIdStr = (await GetApplicationIdAsync(cancellationToken).ConfigureAwait(false))?.ToString("N")[..8] ?? "EUROERP";

        const string upd = "UPDATE [ORDER] SET STATUS = 'R', LAST_ACTV = 'REOPEN', STATUS_CHG_DATE = GETDATE() WHERE PKId = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(upd, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        const string insHist = "INSERT INTO [ORDER_STATUS_HIST] (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, NEW_STATUS, ORDER_ID) VALUES (GETDATE(), @UserId, @ApplicationId, 'R', @OrderId)";
        await _connection.ExecuteAsync(new CommandDefinition(insHist, new { UserId = uid, ApplicationId = appIdStr.Length > 8 ? appIdStr[..8] : appIdStr, OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<bool> HasPaymentAsync(int btrId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT 1 FROM [FINANCE_RECEIVE] WHERE FINANCE_BTR_ID = @BtrId";
        var exists = await _connection.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { BtrId = btrId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return exists.HasValue && exists.Value == 1;
    }

    public async Task<OrderForPrintDto?> GetOrderForPrintAsync(int orderId, byte printType, CancellationToken cancellationToken = default)
    {
        const string sqlHeader = @"
            SELECT o.PKId AS OrderId, c.PKId AS ClientId, o.SYS_CREATION_DATE AS OrderDate, o.RECEIPT AS Receipt, ISNULL(o.MEMO, '') AS Memo,
                o.ML_ORDER_ID AS MlOrderId, o.ML_ORDER_DATE AS MlOrderDate, o.ML_SHIPMENT_COST AS MlShipmentCost,
                ISNULL(o.CREDIT, 0) AS Credit, ISNULL(o.DISCOUNT, 0) AS Discount,
                ISNULL(o.OTHER_EXPENSES, 0) AS OtherExpenses, ISNULL(o.SHIPMENT_COST, 0) AS ShipmentCost,
                c.SOCIAL_NAME AS SocialName, c.FANTASY_NAME AS ClientName, ISNULL(c.CNPJPF, '') AS CnpjPf, ISNULL(c.STATE_INSCR, '') AS StateInscr,
                o.SALES_AGENT AS SalesAgent, c.ADDRESS_STREET AS AddressStreet, ISNULL(c.ADDRESS_NUMBER, '') AS AddressNumber,
                ISNULL(c.ADDRESS_COMPLEMENT, '') AS AddressComplement, ISNULL(c.ADDRESS_BLOCK, '') AS AddressBlock,
                ci.NAME AS City, st.CODE AS State, ISNULL(c.ADDRESS_ZIPCODE, '') AS AddressZipCode,
                ISNULL(c.PHONE1, '') AS Phone1, mkt.CURRENCY_ID AS CurrencyId, cur.SYMBOL AS CurrencySymbol
            FROM [ORDER] o
            JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            JOIN [MARKET] mkt ON mkt.PKId = c.MARKET_ID
            JOIN [CURRENCY] cur ON cur.PKId = mkt.CURRENCY_ID
            LEFT JOIN [CITY] ci ON ci.PKId = c.ADDRESS_CITY_ID
            LEFT JOIN [STATE] st ON st.PKId = c.ADDRESS_STATE_ID
            WHERE o.PKId = @OrderId";
        var header = await _connection.QueryFirstOrDefaultAsync<dynamic>(new CommandDefinition(sqlHeader, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (header == null)
            return null;

        var details = await GetOrderDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        var total = await GetOrderTotalAmountAsync(orderId, cancellationToken).ConfigureAwait(false);

        var lines = details.Select(d => new OrderForPrintLineDto
        {
            LionCode = d.LionCode,
            Name = d.Name,
            Quantity = d.Quantity,
            Price = d.Price,
            Discount = d.Discount,
            TotalPrice = d.TotalPrice
        }).ToList();

        IReadOnlyList<BtrDetailForPrintDto> paymentDetails = Array.Empty<BtrDetailForPrintDto>();
        if (printType == 1)
        {
            const string sqlBtr = @"
                SELECT btrd.TERM_NO AS TermNo, btrd.AMOUNT AS Amount, btrd.DUE_DATE AS DueDate,
                    ISNULL(btrd.MEMO, '') AS Memo, ISNULL(pm.NAME, '') AS PaymentMethodName
                FROM [ORDER] o
                JOIN [FINANCE_BTR] btr ON btr.PKId = o.BTR_ID
                JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
                LEFT JOIN [PAYMENT_METHOD] pm ON pm.PKId = btrd.PAYMENT_METHOD_ID
                WHERE o.PKId = @OrderId";
            var btrRows = await _connection.QueryAsync<BtrDetailForPrintDto>(new CommandDefinition(sqlBtr, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            paymentDetails = btrRows.ToList();
        }

        string mlOrderId = "";
        if (header.MlOrderId != null && header.MlOrderId.ToString()!.Length > 1)
        {
            mlOrderId = "ML #" + header.MlOrderId;
            if (header.MlOrderDate != null)
                mlOrderId += " / " + ((DateTime)header.MlOrderDate).ToString("dd/MM/yyyy");
        }

        return new OrderForPrintDto
        {
            OrderId = orderId,
            ClientId = (int)header.ClientId,
            OrderDate = (DateTime)header.OrderDate,
            ClientName = (string)header.ClientName,
            SocialName = (string)header.SocialName,
            CnpjPf = (string)header.CnpjPf,
            StateInscr = (string)header.StateInscr,
            SalesAgent = header.SalesAgent?.ToString() ?? "",
            AddressStreet = (string)header.AddressStreet,
            AddressNumber = (string)header.AddressNumber,
            AddressComplement = (string)header.AddressComplement,
            AddressBlock = (string)header.AddressBlock,
            City = (string)header.City,
            State = (string)header.State,
            AddressZipCode = (string)header.AddressZipCode,
            Phone1 = (string)header.Phone1,
            Receipt = header.Receipt != null ? (int?)header.Receipt : null,
            Memo = (string)header.Memo,
            MlOrderId = mlOrderId,
            MlOrderDate = header.MlOrderDate != null ? (DateTime?)header.MlOrderDate : null,
            MlShipmentCost = header.MlShipmentCost != null ? (decimal?)header.MlShipmentCost : null,
            Credit = (decimal)header.Credit,
            Discount = (decimal)header.Discount,
            OtherExpenses = (decimal)header.OtherExpenses,
            ShipmentCost = (decimal)header.ShipmentCost,
            Total = total,
            CurrencySymbol = (string)header.CurrencySymbol,
            Details = lines,
            PaymentDetails = paymentDetails
        };
    }

    public async Task<OrderLabelDto?> GetOrderLabelAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT o.PKId AS OrderId, o.ML_ORDER_ID AS MlOrderId,
                c.SOCIAL_NAME AS SocialName, c.ADDRESS_STREET AS AddressStreet,
                ISNULL(c.ADDRESS_NUMBER, '') AS AddressNumber, ISNULL(c.ADDRESS_COMPLEMENT, '') AS AddressComplement,
                ISNULL(c.ADDRESS_BLOCK, '') AS AddressBlock, ci.NAME AS City, st.CODE AS State,
                ISNULL(c.ADDRESS_ZIPCODE, '') AS AddressZipCode, ISNULL(c.CELULAR, c.PHONE1) AS Phone
            FROM [ORDER] o
            JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            LEFT JOIN [CITY] ci ON ci.PKId = c.ADDRESS_CITY_ID
            LEFT JOIN [STATE] st ON st.PKId = c.ADDRESS_STATE_ID
            WHERE o.PKId = @OrderId";
        var row = await _connection.QueryFirstOrDefaultAsync<OrderLabelDto>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row;
    }

    public async Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersByClientAsync(int clientId, int top = 40, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP (@Top)
                o.PKId AS OrderId,
                o.SYS_CREATION_DATE AS CreationDate,
                o.STATUS AS Status,
                c.FANTASY_NAME AS ClientFantasyName
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            WHERE c.PKId = @ClientId
            ORDER BY o.PKId DESC";
        var list = await _connection.QueryAsync<OrderSearchResultDto>(
            new CommandDefinition(sql, new { ClientId = clientId, Top = top }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<OrderSearchResultDto?> SearchOrderByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT o.PKId AS OrderId, o.SYS_CREATION_DATE AS CreationDate, o.STATUS AS Status, c.FANTASY_NAME AS ClientFantasyName
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            WHERE o.PKId = @OrderId";
        return await _connection.QueryFirstOrDefaultAsync<OrderSearchResultDto>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersByReceiptAsync(int receiptNumber, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT o.PKId AS OrderId, o.SYS_CREATION_DATE AS CreationDate, o.STATUS AS Status, c.FANTASY_NAME AS ClientFantasyName
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            WHERE o.RECEIPT = @ReceiptNumber";
        var list = await _connection.QueryAsync<OrderSearchResultDto>(
            new CommandDefinition(sql, new { ReceiptNumber = receiptNumber }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersByMeliAsync(string mlOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mlOrderId)) return new List<OrderSearchResultDto>();
        const string sql = @"
            SELECT o.PKId AS OrderId, o.SYS_CREATION_DATE AS CreationDate, o.STATUS AS Status, c.FANTASY_NAME AS ClientFantasyName
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            WHERE o.ML_ORDER_ID = @MlOrderId";
        var list = await _connection.QueryAsync<OrderSearchResultDto>(
            new CommandDefinition(sql, new { MlOrderId = mlOrderId.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<bool> MlOrderExistsAsync(string mlOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mlOrderId)) return false;
        const string sql = "SELECT 1 FROM [ORDER] WHERE ML_ORDER_ID = @MlOrderId";
        var v = await _connection.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { MlOrderId = mlOrderId.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return v.HasValue && v.Value == 1;
    }

    public async Task UnlinkMlOrderAsync(string mlOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mlOrderId)) return;
        const string sql = "UPDATE [ORDER] SET ML_ORDER_ID = NULL, ML_ORDER_DATE = NULL, ML_SHIPMENT_COST = NULL WHERE ML_ORDER_ID = @MlOrderId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { MlOrderId = mlOrderId.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MlOrderRowDto>> GetOrdersByMlIdsAsync(IReadOnlyList<string> mlOrderIds, CancellationToken cancellationToken = default)
    {
        if (mlOrderIds == null || mlOrderIds.Count == 0) return new List<MlOrderRowDto>();
        var param = new DynamicParameters();
        var inClause = string.Join(",", mlOrderIds.Select((_, i) => "@p" + i));
        for (var i = 0; i < mlOrderIds.Count; i++)
            param.Add("p" + i, mlOrderIds[i].Trim());
        var sql = $"SELECT PKId, NFE_RECEIPT AS NfeReceipt, ML_ORDER_ID AS MlOrderId FROM [ORDER] WHERE ML_ORDER_ID IN ({inClause})";
        var list = await _connection.QueryAsync<MlOrderRowDto>(new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<IReadOnlyList<OrderSearchResultDto>> GetLastClosedOrdersAsync(int top = 30, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP (@Top)
                o.PKId AS OrderId,
                o.SYS_CREATION_DATE AS CreationDate,
                o.STATUS AS Status,
                c.FANTASY_NAME AS ClientFantasyName,
                st.CODE AS StateCode,
                ci.NAME AS CityName
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            LEFT JOIN [STATE] st ON st.PKId = c.ADDRESS_STATE_ID
            LEFT JOIN [CITY] ci ON ci.PKId = c.ADDRESS_CITY_ID
            WHERE o.STATUS = 'F'
            ORDER BY o.PKId DESC";
        var list = await _connection.QueryAsync<OrderSearchResultDto>(
            new CommandDefinition(sql, new { Top = top }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<IReadOnlyList<PendingOrderDto>> GetPendingOrdersAsync(int? productId, CancellationToken cancellationToken = default)
    {
        if (productId.HasValue && productId.Value > 0)
        {
            const string sqlByProduct = @"
                SELECT
                    o.PKId AS OrderId,
                    o.SYS_CREATION_DATE AS CreationDate,
                    c.FANTASY_NAME AS ClientFantasyName,
                    o.SALES_AGENT AS SalesAgent,
                    o.STATUS AS Status,
                    o.ORDER_TYPE AS OrderType,
                    ISNULL(od.QTD, 0) AS Quantity,
                    0 AS Total
                FROM [ORDER] o
                JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
                INNER JOIN (
                    SELECT ORDER_ID, SUM(QUANTITY) AS QTD
                    FROM [ORDER_DETAILS]
                    WHERE PRODUCT_ID = @ProductId AND QUANTITY > 0
                    GROUP BY ORDER_ID
                ) od ON o.PKId = od.ORDER_ID
                WHERE o.STATUS NOT IN ('E','C')
                ORDER BY o.PKId DESC";
            var list = await _connection.QueryAsync<PendingOrderDto>(
                new CommandDefinition(sqlByProduct, new { ProductId = productId.Value }, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return list.ToList();
        }

        const string sqlAll = @"
            SELECT
                o.PKId AS OrderId,
                o.SYS_CREATION_DATE AS CreationDate,
                c.FANTASY_NAME AS ClientFantasyName,
                o.SALES_AGENT AS SalesAgent,
                o.STATUS AS Status,
                o.ORDER_TYPE AS OrderType,
                0 AS Quantity,
                ISNULL(SUM(ROUND(ROUND(ROUND(od.PRICE * od.CONVERSION, 2) * (1 - od.DISCOUNT/100), 2) * od.QUANTITY, 2)), 0) AS Total
            FROM [ORDER] o
            JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            LEFT JOIN [ORDER_DETAILS] od ON o.PKId = od.ORDER_ID AND od.QUANTITY > 0
            WHERE o.STATUS NOT IN ('E','C')
            GROUP BY o.PKId, o.SYS_CREATION_DATE, c.FANTASY_NAME, o.SALES_AGENT, o.STATUS, o.ORDER_TYPE
            ORDER BY o.PKId DESC";
        var listAll = await _connection.QueryAsync<PendingOrderDto>(
            new CommandDefinition(sqlAll, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return listAll.ToList();
    }

    public async Task<SendOrderResult> SendOrderAsync(int orderId, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
            return new SendOrderResult(false, "Número inválido.");

        var header = await GetOrderHeaderAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (header == null)
            return new SendOrderResult(false, $"O pedido {orderId} não existe!");

        var status = header.Status;
        if (status == "E")
            return new SendOrderResult(true, $"Pedido {orderId} já enviado previamente.");

        if (status != "F")
        {
            var statusName = GetOrderStatusName(status);
            return new SendOrderResult(false, $"O status do pedido é: {statusName}. Portanto não é possível enviar.");
        }

        const string sqlCount = "SELECT ISNULL(SUM(od.QUANTITY), 0) FROM [ORDER_DETAILS] od WHERE od.ORDER_ID = @OrderId";
        var count = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(sqlCount, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (count == 0)
            return new SendOrderResult(false, "Impossível enviar compras que não tenham produtos.");

        const string sqlSentDate = "SELECT SENT_DATE FROM [ORDER] WHERE PKId = @OrderId";
        var sentDate = await _connection.ExecuteScalarAsync<DateTime?>(new CommandDefinition(sqlSentDate, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;

        if (!sentDate.HasValue)
        {
            const string upd = "UPDATE [ORDER] SET STATUS = 'E', LAST_ACTV = 'SEND', STATUS_CHG_DATE = GETDATE(), SENT_DATE = GETDATE() WHERE PKId = @OrderId";
            await _connection.ExecuteAsync(new CommandDefinition(upd, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            const string upd = "UPDATE [ORDER] SET STATUS = 'E', LAST_ACTV = 'SEND', STATUS_CHG_DATE = GETDATE() WHERE PKId = @OrderId";
            await _connection.ExecuteAsync(new CommandDefinition(upd, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        const string insHist = "INSERT INTO [ORDER_STATUS_HIST] (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, NEW_STATUS, ORDER_ID) VALUES (GETDATE(), @UserId, @ApplicationId, 'E', @OrderId)";
        await _connection.ExecuteAsync(new CommandDefinition(insHist, new { UserId = uid, ApplicationId = appId, OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new SendOrderResult(true, $"Pedido {orderId} enviado.");
    }

    public async Task<IReadOnlyList<BtrSearchRowDto>> SearchBtrByOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                btrd.AMOUNT AS Amount,
                ISNULL(btr.TERMS, 1) AS Terms,
                ISNULL(pm.NAME, '') AS Label,
                '' AS Psm,
                '01' AS Cfe
            FROM [ORDER] o
            JOIN [FINANCE_BTR] btr ON btr.PKId = o.BTR_ID
            JOIN [FINANCE_BTR_DETAIL] btrd ON btrd.FINANCE_BTR_ID = btr.PKId
            LEFT JOIN [PAYMENT_METHOD] pm ON pm.PKId = btrd.PAYMENT_METHOD_ID
            WHERE o.PKId = @OrderId
              AND o.STATUS IN ('F', 'E')
            ORDER BY btrd.TERM_NO";
        var rows = await _connection.QueryAsync<BtrSearchRowDto>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<ClientByOrderDto?> GetClientByOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                c.FANTASY_NAME AS ClientName,
                ISNULL(c.EMAIL, '') AS Email,
                ISNULL(o.TRACK, '') AS Track,
                ISNULL(o.VIA, '') AS Via,
                ISNULL(o.NFE_KEY, '') AS NfeKey,
                ISNULL(o.RECEIPT, '') AS Receipt
            FROM [ORDER] o
            JOIN [ORDER_DETAILS] od ON od.ORDER_ID = o.PKId
            JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            LEFT JOIN [RECEIPT_CANCEL] rc ON rc.RECEIPT_NO = o.RECEIPT
            WHERE o.PKId = @OrderId
            GROUP BY c.FANTASY_NAME, c.EMAIL, o.TRACK, o.VIA, o.NFE_KEY, o.RECEIPT";
        var row = await _connection.QueryFirstOrDefaultAsync<ClientByOrderDto>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row;
    }

    public async Task<OrderByNfeKeyDto?> GetOrderByNfeKeyAsync(string nfeKey, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                CAST(o.PKId AS NVARCHAR(20)) AS OrderId,
                ISNULL(c.FANTASY_NAME, '') AS ClientName,
                ISNULL(c.EMAIL, '') AS Email,
                ISNULL(CAST(o.SITE_ORDER_ID AS NVARCHAR(50)), '') AS SiteOrderId,
                ISNULL(o.ML_ORDER_ID, '') AS MlOrderId
            FROM [ORDER] o
            JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            WHERE o.NFE_KEY = @NfeKey";
        var key = nfeKey ?? string.Empty;
        var row = await _connection.QueryFirstOrDefaultAsync<OrderByNfeKeyDto>(
            new CommandDefinition(sql, new { NfeKey = key }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row;
    }

    public async Task<IReadOnlyList<LastNfeOrderDto>> GetLastNfeOrdersPendingShipmentAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT o.PKId AS OrderId,
                o.RECEIPT AS ReceiptNo,
                o.NFE_KEY AS NfeKey,
                c.SOCIAL_NAME AS SocialName,
                c.EMAIL AS Email
            FROM [ORDER] o
            JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
            WHERE o.STATUS = 'E'
                AND o.TRACKER_IND IS NULL
                AND o.NFE_KEY IS NOT NULL
                AND c.EMAIL IS NOT NULL
                AND o.SENT_DATE > GETDATE() - 7";
        var list = await _connection.QueryAsync<LastNfeOrderDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<bool> UpdateOrderViaAsync(int orderId, string via, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE [ORDER] SET VIA = @Via WHERE PKId = @OrderId";
        var rows = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { OrderId = orderId, Via = via ?? string.Empty }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<bool> UpdateOrderTrackAsync(int orderId, string? track, string? via, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(track) && string.IsNullOrEmpty(via))
            return false;
        const string check = "SELECT 1 FROM [ORDER] WHERE PKId = @OrderId";
        var exists = await _connection.ExecuteScalarAsync<int?>(new CommandDefinition(check, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (exists != 1)
            return false;
        if (!string.IsNullOrEmpty(via))
        {
            const string sqlVia = "UPDATE [ORDER] SET VIA = @Via WHERE PKId = @OrderId";
            await _connection.ExecuteAsync(new CommandDefinition(sqlVia, new { OrderId = orderId, Via = via }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        if (!string.IsNullOrEmpty(track))
        {
            const string sqlTrack = "UPDATE [ORDER] SET TRACK = @Track WHERE PKId = @OrderId";
            await _connection.ExecuteAsync(new CommandDefinition(sqlTrack, new { OrderId = orderId, Track = track }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        const string sqlProcessed = "UPDATE [ORDER] SET TRACKER_IND = @TrackerInd WHERE PKId = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(sqlProcessed, new { OrderId = orderId, TrackerInd = "P" }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return true;
    }

    private static string GetOrderStatusName(string status) => status switch
    {
        "I" => "Incompleto",
        "R" => "Reaberto",
        "F" => "Fechado",
        "C" => "Cancelado",
        "E" => "Enviado",
        _ => "Indefinido"
    };

    private sealed class DelinqRow
    {
        public DateTime AllowDelinq { get; init; }
        public bool IgnoreDelinq { get; init; }
    }
}
