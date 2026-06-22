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
    private readonly INfesSimplissClient _simplissClient;
    private readonly ILogger<NfesEmissionService> _logger;

    public NfesEmissionService(
        IDbConnection connection,
        INfesConfigProvider configProvider,
        PrefeituraSpNfesBackend prefeituraSpBackend,
        SimplissNfesBackend simplissBackend,
        INfesSimplissClient simplissClient,
        ILogger<NfesEmissionService> logger)
    {
        _connection = connection;
        _configProvider = configProvider;
        _prefeituraSpBackend = prefeituraSpBackend;
        _simplissBackend = simplissBackend;
        _simplissClient = simplissClient;
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

                ISNULL(o.RPS_NO, '') AS RpsNo,

                ISNULL(o.NFES_CHECK_CODE, '') AS NfesCheckCode,

                ISNULL(o.NFES_CHAVE_ACESSO, '') AS NfesChaveAcesso

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

            NfesCheckCode = string.IsNullOrWhiteSpace(row.NfesNo) || row.NfesNo == "0"
                ? null
                : (string.IsNullOrWhiteSpace(row.NfesCheckCode) ? null : row.NfesCheckCode.Trim()),

            NfesChaveAcesso = string.IsNullOrWhiteSpace(row.NfesNo) || row.NfesNo == "0"
                ? null
                : ResolveSimplissChave(row.NfesChaveAcesso, row.NfesCheckCode, nfesConfig.UseSimpliss),

            Provider = nfesConfig.Provider

        };

        preview.PrintUrl = BuildPrintUrl(nfesConfig, orderId, preview.NfesNo, preview.NfesCheckCode, preview.NfesChaveAcesso);



        var nfesCanceled = !string.IsNullOrEmpty(preview.NfesNo)
            && await IsNfesReceiptCanceledAsync(preview.NfesNo, cancellationToken).ConfigureAwait(false);

        if (nfesCanceled)
        {
            preview.NfesNo = null;
            preview.RpsNo = null;
            preview.NfesCheckCode = null;
            preview.NfesChaveAcesso = null;
            preview.PrintUrl = null;
        }

        var hasActiveNfes = !string.IsNullOrEmpty(preview.NfesNo);

        if (preview.Status is not ("F" or "E"))

            preview.BlockReason = "O status da compra não permite geração de NF.";

        else if (hasActiveNfes)

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



    private async Task<int> ResolveRpsNumberAsync(string requestRps, bool reprint, CancellationToken cancellationToken)

    {

        if (!reprint)

            return await GetNextRpsNumberAsync(cancellationToken).ConfigureAwait(false);



        var rpsText = requestRps.Trim();

        if (rpsText.StartsWith("R", StringComparison.OrdinalIgnoreCase))

            rpsText = rpsText[1..];

        if (int.TryParse(rpsText, out var rpsNumber) && rpsNumber > 0)

            return rpsNumber;



        return await GetNextRpsNumberAsync(cancellationToken).ConfigureAwait(false);

    }



    private async Task SetSysControlRpsNoAsync(int nextRps, IDbTransaction? tx, CancellationToken cancellationToken)

    {

        const string sql = "UPDATE SYS_CONTROL SET value = @Value WHERE CODE = 'RPS_NO'";

        await _connection.ExecuteAsync(

            new CommandDefinition(sql, new { Value = nextRps.ToString(CultureInfo.InvariantCulture) }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

    }



    private static bool IsDuplicateDpsError(string? message) =>

        message?.Contains("E0014", StringComparison.OrdinalIgnoreCase) == true;



    public async Task<EmitNfesResult> EmitAsync(EmitNfesRequest request, CancellationToken cancellationToken = default)

    {

        var preview = await GetOrderPreviewAsync(request.OrderId, cancellationToken).ConfigureAwait(false);

        if (preview == null)

            return Fail("Pedido não encontrado.");



        var reprint = request.RpsNumber.TrimStart().StartsWith("R", StringComparison.OrdinalIgnoreCase);

        if (!reprint && !preview.CanEmit)

            return Fail(preview.BlockReason ?? "Emissão não permitida.");



        var rpsNumber = await ResolveRpsNumberAsync(request.RpsNumber, reprint, cancellationToken).ConfigureAwait(false);



        try

        {

            if (_connection.State != ConnectionState.Open)

                _connection.Open();



            for (var attempt = 0; attempt < 2; attempt++)

            {

                using var tx = _connection.BeginTransaction();

                try

                {

                    var result = await EmitCoreAsync(request.OrderId, rpsNumber, reprint, tx, cancellationToken).ConfigureAwait(false);

                    if (result.Success)

                    {

                        tx.Commit();

                        return result;

                    }



                    tx.Rollback();



                    if (attempt == 0 && !reprint && IsDuplicateDpsError(result.Message))

                    {

                        rpsNumber++;

                        _logger.LogWarning(

                            "NFES E0014 on order {OrderId}; retrying with RPS {RpsNumber} (counter advances only after success)",

                            request.OrderId, rpsNumber);

                        continue;

                    }



                    return result;

                }

                catch

                {

                    tx.Rollback();

                    throw;

                }

            }



            return Fail("Não foi possível emitir a NFS-e após tentativa com novo número RPS.");

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
            UPDATE [ORDER]
            SET RPS_NO = @RpsNo,
                NFES_NO = @NfesNo,
                NFES_CHECK_CODE = @CheckCode,
                NFES_CHAVE_ACESSO = CASE
                    WHEN @ChaveAcesso IS NOT NULL AND @ChaveAcesso <> '' THEN @ChaveAcesso
                    ELSE NFES_CHAVE_ACESSO
                END
            WHERE PKId = @OrderId";

        var chaveAcesso = nfesConfig.UseSimpliss ? NfesTextHelper.FitDb(outcome.ChaveAcesso, 100) : null;

        await _connection.ExecuteAsync(

            new CommandDefinition(updOrder, new

            {

                OrderId = orderId,

                RpsNo = NfesTextHelper.FitDb(outcome.RpsNo, 15),

                NfesNo = NfesTextHelper.FitDb(outcome.NfesNo, 15),

                CheckCode = NfesTextHelper.FitDb(outcome.CheckCode, 20),

                ChaveAcesso = string.IsNullOrEmpty(chaveAcesso) ? null : chaveAcesso

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

            await SetSysControlRpsNoAsync(rpsNumber + 1, tx, cancellationToken).ConfigureAwait(false);

        }



        return new EmitNfesResult

        {

            Success = true,

            Message = $"{outcome.Message} NFS-e {outcome.NfesNo}, RPS {outcome.RpsNo}.",

            NfesNo = outcome.NfesNo,

            RpsNo = outcome.RpsNo,

            CheckCode = outcome.CheckCode,

            NfesChaveAcesso = outcome.ChaveAcesso,

            PrintUrl = BuildPrintUrl(nfesConfig, orderId, outcome.NfesNo, outcome.CheckCode, outcome.ChaveAcesso, outcome.PdfUrl),

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



    public async Task<NfesPrintPdfResult> GetDanfsePdfAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var preview = await GetOrderPreviewAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (preview == null)
            return FailPrint("Pedido não encontrado.");

        var nfesConfig = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!nfesConfig.UseSimpliss)
            return FailPrint("Impressão via ERP disponível apenas para NFS-e Simpliss (layout nacional).");

        var chave = preview.NfesChaveAcesso;
        if (string.IsNullOrWhiteSpace(chave))
            return FailPrint("Pedido sem chave de acesso NFS-e para impressão.");

        string? xml = await TryLoadLocalNfseXmlAsync(orderId, nfesConfig, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(xml))
        {
            var consult = await _simplissClient.GetNfseByChaveAsync(chave, cancellationToken).ConfigureAwait(false);
            if (!consult.Success || string.IsNullOrWhiteSpace(consult.NfseXml))
                return FailPrint(consult.ErrorMessage ?? "Não foi possível obter a NFS-e no webservice Simpliss.");

            xml = consult.NfseXml;
            await TrySaveLocalNfseXmlAsync(orderId, nfesConfig, xml, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var pdf = NfesDanfsePdfGenerator.Generate(xml, nfesConfig);
            var fileName = $"danfse_{orderId}_{preview.NfesNo ?? "nfse"}.pdf";
            return new NfesPrintPdfResult { Success = true, PdfBytes = pdf, FileName = fileName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DANFSe PDF generation failed for order {OrderId}", orderId);
            return FailPrint($"Falha ao gerar PDF: {ex.Message}");
        }
    }

    private static async Task<string?> TryLoadLocalNfseXmlAsync(int orderId, NfesConfigSnapshot config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.XmlPath))
            return null;

        var path = Path.Combine(config.XmlPath, "S" + orderId, orderId + "-nfse.xml");
        if (!File.Exists(path))
            return null;

        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }

    private static async Task TrySaveLocalNfseXmlAsync(int orderId, NfesConfigSnapshot config, string xml, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.XmlPath))
            return;

        try
        {
            var folder = Path.Combine(config.XmlPath, "S" + orderId);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, orderId + "-nfse.xml");
            if (!File.Exists(path))
                await File.WriteAllTextAsync(path, xml, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal: printing still succeeded.
        }
    }

    private static string? BuildPrintUrl(
        NfesConfigSnapshot config,
        int orderId,
        string? nfesNo,
        string? checkCode,
        string? chaveAcesso,
        string? pdfUrl = null)
    {
        if (config.UseSimpliss && orderId > 0 && !string.IsNullOrWhiteSpace(chaveAcesso))
            return $"/api/nfes/imprimir?orderId={orderId}";

        return NfesPrintUrlBuilder.Build(config, nfesNo, checkCode, pdfUrl, chaveAcesso);
    }

    private static NfesPrintPdfResult FailPrint(string message) =>
        new() { Success = false, Message = message };

    private static EmitNfesResult Fail(string message) => new() { Success = false, Message = message };



    private static string? ResolveSimplissChave(string? nfesChaveAcesso, string? checkCode, bool useSimpliss)
    {
        if (!useSimpliss)
            return null;

        foreach (var candidate in new[] { nfesChaveAcesso, checkCode })
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;
            var trimmed = candidate.Trim();
            if (trimmed.StartsWith("NFS", StringComparison.OrdinalIgnoreCase))
                return trimmed;
            var digits = NfesTextHelper.CleanDigits(trimmed);
            if (digits.Length >= 50)
                return digits.Length == 50 ? digits : digits[^50..];
        }

        return null;
    }



    private async Task<bool> IsNfesReceiptCanceledAsync(string nfesNo, CancellationToken cancellationToken)

    {

        if (!int.TryParse(nfesNo.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var receiptNo) || receiptNo < 1)

            return false;

        const string sql = "SELECT 1 FROM RECEIPT_CANCEL WHERE RECEIPT_NO = @ReceiptNo";

        var exists = await _connection.ExecuteScalarAsync<int?>(

            new CommandDefinition(sql, new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return exists == 1;

    }



    private sealed class OrderPreviewRow

    {

        public int OrderId { get; init; }

        public string? Status { get; init; }

        public int ClientId { get; init; }

        public string? ClientName { get; init; }

        public string? NfesNo { get; init; }

        public string? RpsNo { get; init; }

        public string? NfesCheckCode { get; init; }

        public string? NfesChaveAcesso { get; init; }

    }

}


