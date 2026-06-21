using System.Data;

using System.Globalization;

using Dapper;

using EUROERP.Application.Nfes;
using Microsoft.Extensions.Logging;



namespace EUROERP.Infrastructure.Nfes;



public class NfesEmissionService : INfesEmissionService

{

    private const byte Category = 100;



    private readonly IDbConnection _connection;
    private readonly INfesConfigProvider _configProvider;
    private readonly PrefeituraSpNfesBackend _prefeituraSpBackend;
    private readonly SimplissNfesBackend _simplissBackend;
    private readonly ILogger<NfesEmissionService> _logger;

    public NfesEmissionService(
        IDbConnection connection,
        INfesConfigProvider configProvider,
        PrefeituraSpNfesBackend prefeituraSpBackend,
        SimplissNfesBackend simplissBackend,
        ILogger<NfesEmissionService> logger)
    {
        _connection = connection;
        _configProvider = configProvider;
        _prefeituraSpBackend = prefeituraSpBackend;
        _simplissBackend = simplissBackend;
        _logger = logger;
    }



    public async Task<NfesOrderPreviewDto?> GetOrderPreviewAsync(int orderId, CancellationToken cancellationToken = default)

    {

        const string sql = @"

            SELECT

                o.PKId AS OrderId,

                o.STATUS AS Status,

                c.PKId AS ClientId,

                c.SOCIAL_NAME AS ClientName,

                ISNULL(o.NFES_NO, '') AS NfesNo,

                ISNULL(o.RPS_NO, '') AS RpsNo

            FROM [ORDER] o

            JOIN CLIENT c ON c.PKId = o.CLIENT_ID

            WHERE o.PKId = @OrderId";



        var row = await _connection.QuerySingleOrDefaultAsync<OrderPreviewRow>(

            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row == null)

            return null;



        var serviceTotal = await GetServiceNetTotalAsync(orderId, null, cancellationToken).ConfigureAwait(false);
        var nfesConfig = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var preview = new NfesOrderPreviewDto

        {

            OrderId = row.OrderId,

            Status = row.Status?.Trim() ?? "",

            ClientId = row.ClientId,

            ClientName = row.ClientName ?? "",

            ServiceTotal = serviceTotal,

            NfesNo = string.IsNullOrWhiteSpace(row.NfesNo) || row.NfesNo == "0" ? null : row.NfesNo.Trim(),

            RpsNo = string.IsNullOrWhiteSpace(row.RpsNo) || row.RpsNo == "0" ? null : row.RpsNo.Trim(),

            Provider = nfesConfig.Provider

        };



        if (preview.Status is not ("F" or "E"))

            preview.BlockReason = "O status da compra não permite geração de NF.";

        else if (!string.IsNullOrEmpty(preview.NfesNo))

            preview.BlockReason = "NFE Serviço já emitida para este pedido.";

        else if (serviceTotal <= 0)

            preview.BlockReason = "Pedido sem valor de serviços.";

        else

            preview.CanEmit = true;



        return preview;

    }



    public async Task<int> GetNextRpsNumberAsync(CancellationToken cancellationToken = default)

    {

        const string sql = "SELECT CAST(value AS INT) FROM SYS_CONTROL WHERE CODE = 'RPS_NO'";

        var n = await _connection.ExecuteScalarAsync<int?>(

            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return n ?? 1;

    }



    public async Task<EmitNfesResult> EmitAsync(EmitNfesRequest request, CancellationToken cancellationToken = default)

    {

        var preview = await GetOrderPreviewAsync(request.OrderId, cancellationToken).ConfigureAwait(false);

        if (preview == null)

            return Fail("Pedido não encontrado.");



        var reprint = request.RpsNumber.TrimStart().StartsWith("R", StringComparison.OrdinalIgnoreCase);

        if (!reprint && !preview.CanEmit)

            return Fail(preview.BlockReason ?? "Emissão não permitida.");



        var rpsText = request.RpsNumber.Trim();

        if (rpsText.StartsWith("R", StringComparison.OrdinalIgnoreCase))

            rpsText = rpsText[1..];

        if (!int.TryParse(rpsText, out var rpsNumber) || rpsNumber <= 0)

            rpsNumber = await GetNextRpsNumberAsync(cancellationToken).ConfigureAwait(false);



        try

        {

            if (_connection.State != ConnectionState.Open)

                _connection.Open();



            using var tx = _connection.BeginTransaction();

            try

            {

                var result = await EmitCoreAsync(request.OrderId, rpsNumber, reprint, tx, cancellationToken).ConfigureAwait(false);

                if (result.Success)

                    tx.Commit();

                else

                    tx.Rollback();

                return result;

            }

            catch

            {

                tx.Rollback();

                throw;

            }

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "NFES emission failed for order {OrderId}", request.OrderId);

            return Fail(ex.Message);

        }

    }



    private async Task<EmitNfesResult> EmitCoreAsync(

        int orderId,

        int rpsNumber,

        bool reprint,

        IDbTransaction tx,

        CancellationToken cancellationToken)

    {

        var order = await LoadOrderAsync(orderId, tx, cancellationToken).ConfigureAwait(false)

            ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (order.Status is not ("F" or "E"))

            return Fail("O status da compra não permite geração de NF.");



        var client = await LoadClientAsync(order.ClientId, tx, cancellationToken).ConfigureAwait(false)

            ?? throw new InvalidOperationException("Cliente não encontrado.");



        var netAmount = await GetServiceNetTotalAsync(orderId, tx, cancellationToken).ConfigureAwait(false);

        netAmount = decimal.Round(netAmount * Category / 100m, 2);

        if (netAmount <= 0)

            return Fail("Pedido sem valor de serviços.");



        var work = new NfesEmissionWorkItem

        {

            OrderId = orderId,

            RpsNumber = rpsNumber,

            Reprint = reprint,

            NetAmount = netAmount,

            Order = order,

            Client = client,

            ServiceLines = await LoadServiceLinesAsync(orderId, tx, cancellationToken).ConfigureAwait(false),

            BtrDueDates = await LoadBtrDueDatesAsync(orderId, tx, cancellationToken).ConfigureAwait(false)

        };



        var nfesConfig = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var outcome = nfesConfig.UseSimpliss
            ? await _simplissBackend.EmitAsync(work, cancellationToken).ConfigureAwait(false)
            : await _prefeituraSpBackend.EmitAsync(work, cancellationToken).ConfigureAwait(false);

        if (!outcome.Success)

            return Fail(outcome.Message);



        const string updOrder = @"

            UPDATE [ORDER] SET RPS_NO = @RpsNo, NFES_NO = @NfesNo, NFES_CHECK_CODE = @CheckCode

            WHERE PKId = @OrderId";

        await _connection.ExecuteAsync(

            new CommandDefinition(updOrder, new

            {

                OrderId = orderId,

                RpsNo = outcome.RpsNo,

                NfesNo = outcome.NfesNo,

                CheckCode = outcome.CheckCode ?? ""

            }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);



        if (order.Status == "F")

        {

            const string sendOrder = @"

                UPDATE [ORDER] SET STATUS = 'E', LAST_ACTV = 'SEND', STATUS_CHG_DATE = GETDATE(), SENT_DATE = GETDATE()

                WHERE PKId = @OrderId AND STATUS = 'F'";

            await _connection.ExecuteAsync(

                new CommandDefinition(sendOrder, new { OrderId = orderId }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

        }



        var receiptNo = int.TryParse(outcome.NfesNo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedNo)

            ? parsedNo

            : rpsNumber;



        const string insReceipt = @"

            INSERT INTO RECEIPT (RECEIPT_NO, RECEIPT_FORM_NO, ORDER_ID, SHIPMENT, DELIVERY_SUPPLIER_ID, MSG_ID, CFOP_ID, TYPE, NF_AMOUNT, SYS_CREATION_DATE, CATEGORY)

            VALUES (@ReceiptNo, 0, @OrderId, 0, 0, 0, 0, 'S', @NfAmount, GETDATE(), @Category)";

        await _connection.ExecuteAsync(

            new CommandDefinition(insReceipt, new

            {

                ReceiptNo = receiptNo,

                OrderId = orderId,

                NfAmount = netAmount,

                Category

            }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);



        if (!reprint)

        {

            const string updRps = "UPDATE SYS_CONTROL SET value = @NextRps WHERE CODE = 'RPS_NO'";

            await _connection.ExecuteAsync(

                new CommandDefinition(updRps, new { NextRps = (rpsNumber + 1).ToString(CultureInfo.InvariantCulture) }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

        }



        return new EmitNfesResult

        {

            Success = true,

            Message = $"{outcome.Message} NFS-e {outcome.NfesNo}, RPS {outcome.RpsNo}.",

            NfesNo = outcome.NfesNo,

            RpsNo = outcome.RpsNo,

            CheckCode = outcome.CheckCode,

            XmlResponsePath = outcome.XmlPath

        };

    }



    private async Task<decimal> GetServiceNetTotalAsync(int orderId, IDbTransaction? tx, CancellationToken cancellationToken)

    {

        const string sql = @"

            SELECT ISNULL((

                SELECT TOP 1

                    sum(t1.TOTAL) + sum(t1.TOTAL*(t1.IOD-1))*max(t1.DISC)/100 - max(t1.CREDIT) + MAX(t1.OE) AS CONVERTED_TOTAL_NET_PRICE_NO_SHIP

                FROM (

                    SELECT

                        cast((sum(round(round(round(price*conversion,2)*(1-od.discount/100),2)*quantity,2,1))) as decimal(14,2)) AS TOTAL,

                        isnull((select cast((sum(round(round(round(od2.price*od2.conversion,2)*(1-od2.discount/100),2)*od2.quantity,2,1))) as decimal(14,2))

                            from order_details od2

                            join PRODUCT p2 on p2.PKId=od2.PRODUCT_ID

                            join PRODUCT_GROUP pg2 on pg2.PKId=p2.GROUP_ID

                            join [order] o2 on o2.PKId=od2.ORDER_ID

                            where od2.order_id = @OrderId and pg2.PRODUCT_CLASS_ID = 2)/

                            nullif((select cast((sum(round(round(round(od2.price*od2.conversion,2)*(1-od2.discount/100),2)*od2.quantity,2,1))) as decimal(14,2))

                            from order_details od2

                            join PRODUCT p2 on p2.PKId=od2.PRODUCT_ID

                            join PRODUCT_GROUP pg2 on pg2.PKId=p2.GROUP_ID

                            join [order] o2 on o2.PKId=od2.ORDER_ID

                            where od2.quantity>0 and od2.order_id = @OrderId),0)*o.CREDIT,0) as CREDIT,

                        (isNull(od.ignore_order_disc,convert(bit,0))-1)*-1*isNull(o.DISCOUNT,0) as DISC,

                        isnull(od.ignore_order_disc,convert(bit,0)) as IOD,

                        ISNULL(o.OTHER_EXPENSES,0) as OE

                    FROM order_details od

                    JOIN PRODUCT p on p.PKId=od.PRODUCT_ID

                    JOIN PRODUCT_GROUP pg on pg.PKId=p.GROUP_ID

                    JOIN [order] o on o.PKId=od.ORDER_ID

                    WHERE od.order_id = @OrderId AND pg.PRODUCT_CLASS_ID = 2

                    GROUP BY o.credit, o.DISCOUNT, od.IGNORE_ORDER_DISC, o.OTHER_EXPENSES

                ) t1

                GROUP BY t1.IOD

            ), 0)";



        return await _connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { OrderId = orderId }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

    }



    private async Task<NfesOrderRow?> LoadOrderAsync(int orderId, IDbTransaction tx, CancellationToken ct)

    {

        const string sql = @"

            SELECT o.PKId AS OrderId, o.CLIENT_ID AS ClientId, o.STATUS AS Status,

                car.DESCRIPTION AS CarDescription, car.PLATE AS CarPlate, o.CAR_PROBLEM AS CarProblem

            FROM [ORDER] o

            LEFT JOIN CAR car ON car.PKId = o.CAR_ID

            WHERE o.PKId = @OrderId";

        return await _connection.QuerySingleOrDefaultAsync<NfesOrderRow>(

            new CommandDefinition(sql, new { OrderId = orderId }, tx, cancellationToken: ct)).ConfigureAwait(false);

    }



    private async Task<NfesClientRow?> LoadClientAsync(int clientId, IDbTransaction tx, CancellationToken ct)

    {

        const string sql = @"

            SELECT c.CNPJPF AS CnpjPf, c.PERSON_TYPE AS PersonType, c.SOCIAL_NAME AS SocialName,

                c.ADDRESS_STREET AS AddressStreet, c.ADDRESS_BLOCK AS AddressBlock,

                c.ADDRESS_NUMBER AS AddressNumber, c.ADDRESS_COMPLEMENT AS AddressComplement,

                c.ADDRESS_ZIPCODE AS AddressZipCode, ISNULL(ct.C_MUN,'') AS CMun,

                st.CODE AS AddressStateCode, c.STATE_INSCR AS StateInscr, c.MUN_INSCR AS MunInscr,

                c.EMAIL AS Email, ISNULL(c.PHONE1, ISNULL(c.CELULAR, '')) AS Phone

            FROM CLIENT c

            JOIN STATE st ON st.PKId = c.ADDRESS_STATE_ID

            JOIN CITY ct ON ct.PKId = c.ADDRESS_CITY_ID

            WHERE c.PKId = @ClientId";

        return await _connection.QuerySingleOrDefaultAsync<NfesClientRow>(

            new CommandDefinition(sql, new { ClientId = clientId }, tx, cancellationToken: ct)).ConfigureAwait(false);

    }



    private async Task<List<NfesServiceLineRow>> LoadServiceLinesAsync(int orderId, IDbTransaction tx, CancellationToken ct)

    {

        const string sql = @"

            SELECT p.NAME AS Name, od.QUANTITY AS Quantity,

                CAST(ROUND(ROUND(od.PRICE*od.CONVERSION,2)*(1-od.DISCOUNT/100),2) AS DECIMAL(14,2)) AS UnitPrice,

                CAST(ROUND(ROUND(ROUND(od.PRICE*od.CONVERSION,2)*(1-od.DISCOUNT/100),2)*od.QUANTITY,2) AS DECIMAL(14,2)) AS TotalPrice

            FROM ORDER_DETAILS od

            JOIN PRODUCT p ON p.PKId = od.PRODUCT_ID

            JOIN PRODUCT_GROUP pg ON pg.PKId = p.GROUP_ID

            WHERE od.ORDER_ID = @OrderId AND od.QUANTITY > 0 AND pg.PRODUCT_CLASS_ID = 2

            ORDER BY p.NAME";

        var list = await _connection.QueryAsync<NfesServiceLineRow>(

            new CommandDefinition(sql, new { OrderId = orderId }, tx, cancellationToken: ct)).ConfigureAwait(false);

        return list.ToList();

    }



    private async Task<List<DateTime>> LoadBtrDueDatesAsync(int orderId, IDbTransaction tx, CancellationToken ct)

    {

        const string sql = @"

            SELECT fbtrd.DUE_DATE AS DueDate

            FROM FINANCE_BTR_DETAIL fbtrd

            JOIN FINANCE_BTR fbtr ON fbtr.PKId = fbtrd.FINANCE_BTR_ID

            JOIN [ORDER] o ON o.BTR_ID = fbtr.PKId

            WHERE o.PKId = @OrderId

            ORDER BY fbtrd.TERM_NO";

        var list = await _connection.QueryAsync<DateTime>(

            new CommandDefinition(sql, new { OrderId = orderId }, tx, cancellationToken: ct)).ConfigureAwait(false);

        return list.ToList();

    }



    private static EmitNfesResult Fail(string message) => new() { Success = false, Message = message };



    private sealed class OrderPreviewRow

    {

        public int OrderId { get; init; }

        public string? Status { get; init; }

        public int ClientId { get; init; }

        public string? ClientName { get; init; }

        public string? NfesNo { get; init; }

        public string? RpsNo { get; init; }

    }

}


