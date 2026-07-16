using System.Data;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Dapper;
using EUROERP.Application.NFe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.NFe;

public class NfeIndividualService : INfeIndividualService
{
    /// <summary>SEFAZ rejeição 598: em homologação (tpAmb=2) o xNome do destinatário deve ser exatamente este texto.</summary>
    private const string HomologDestRazaoSocial = "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL";
    private readonly IDbConnection _connection;
    private readonly IConfiguration _configuration;
    private readonly INfeXmlBuilder _xmlBuilder;
    private readonly INfeXmlSigner _xmlSigner;
    private readonly INfeSchemaValidator _schemaValidator;
    private readonly INfeSefazClient _sefazClient;
    private readonly INfeFileStorage _fileStorage;
    private readonly INfePdfGenerator _pdfGenerator;
    private readonly ILogger<NfeIndividualService> _logger;

    public NfeIndividualService(
        IDbConnection connection,
        IConfiguration configuration,
        INfeXmlBuilder xmlBuilder,
        INfeXmlSigner xmlSigner,
        INfeSchemaValidator schemaValidator,
        INfeSefazClient sefazClient,
        INfeFileStorage fileStorage,
        INfePdfGenerator pdfGenerator,
        ILogger<NfeIndividualService> logger)
    {
        _connection = connection;
        _configuration = configuration;
        _xmlBuilder = xmlBuilder;
        _xmlSigner = xmlSigner;
        _schemaValidator = schemaValidator;
        _sefazClient = sefazClient;
        _fileStorage = fileStorage;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Returns current date/time in the NFe timezone (from NFe:GmtOffset, e.g. "-03:00" for Brasília),
    /// formatted for dhEmi/dhEvento. Use this so production (server in UTC) sends correct local time to SEFAZ.
    /// </summary>
    private string GetNfeDateTimeNow()
    {
        var gmt = _configuration["NFe:GmtOffset"] ?? "-03:00";
        var offsetHours = ParseGmtOffsetToHours(gmt);
        var timeInZone = DateTime.UtcNow.AddHours(offsetHours);
        return timeInZone.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + gmt;
    }

    /// <summary>
    /// Parses GmtOffset string (e.g. "-03:00", "+05:30") to offset in hours from UTC.
    /// </summary>
    private static double ParseGmtOffsetToHours(string gmtOffset)
    {
        if (string.IsNullOrWhiteSpace(gmtOffset))
            return -3; // Brasília default
        var s = gmtOffset.Trim();
        var sign = 1;
        if (s.StartsWith("-", StringComparison.Ordinal))
        {
            sign = -1;
            s = s[1..].Trim();
        }
        else if (s.StartsWith("+", StringComparison.Ordinal))
            s = s[1..].Trim();
        var parts = s.Split(':');
        if (parts.Length < 2 || !int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var h)
            || !int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var m))
            return -3;
        return sign * (h + m / 60.0);
    }

    public async Task<int> GetNextReceiptNoAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT VALUE FROM SYS_CONTROL WHERE CODE = 'RECEIPT_NO'";
        var value = await _connection.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(value) || !int.TryParse(value.Trim(), out var n))
            return 1;
        return n;
    }

    public async Task<OrderInfoForNfeDto?> GetOrderInfoForNfeAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                c.SOCIAL_NAME AS ClientName,
                o.STATUS AS Status,
                ISNULL(o.RECEIPT, 0) AS Receipt,
                ISNULL(o.NFE_RECEIPT, '') AS NfeReceipt,
                ISNULL(o.NFE_PROTOCOL_RESULT, '') AS NfeProtocolResult,
                ISNULL(o.NFE_PROTOCOL, '') AS NfeProtocol,
                ISNULL(o.NFE_KEY, '') AS NfeKey,
                (SELECT SUM(od.QUANTITY) FROM [ORDER_DETAILS] od WHERE od.ORDER_ID = o.PKId AND od.QUANTITY > 0) AS ProductCount,
                (c.ADDRESS_STREET + ', ' + ISNULL(c.ADDRESS_NUMBER, '') + ' ' + ISNULL(c.ADDRESS_COMPLEMENT, '') + ' - ' + ISNULL(ci.NAME, '') + '/' + ISNULL(st.CODE, '')) AS Address,
                ISNULL(o.SHIPMENT_COST, 0) AS ShipmentCost,
                ISNULL(o.DISCOUNT, 0) AS Discount,
                ISNULL(o.CREDIT, 0) AS Credit,
                ISNULL(o.OTHER_EXPENSES, 0) AS OtherExpenses
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            LEFT JOIN [CITY] ci ON ci.PKId = c.ADDRESS_CITY_ID
            LEFT JOIN [STATE] st ON st.PKId = c.ADDRESS_STATE_ID
            WHERE o.PKId = @OrderId";
        var row = await _connection.QuerySingleOrDefaultAsync<OrderInfoRow>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null)
            return null;

        if (row.Status != "F" && row.Status != "E")
            return null;
        if (row.ProductCount == null || row.ProductCount == 0)
            return null;

        var total = await GetOrderTotalAsync(orderId, cancellationToken).ConfigureAwait(false);
        var cfopList = await GetOrderCfopListAsync(orderId, cancellationToken).ConfigureAwait(false);

        return new OrderInfoForNfeDto
        {
            ClientName = row.ClientName ?? "",
            Address = row.Address,
            Status = row.Status ?? "",
            OrderTotal = total,
            ShipmentCost = row.ShipmentCost ?? 0,
            Discount = row.Discount ?? 0,
            Credit = row.Credit ?? 0,
            OtherExpenses = row.OtherExpenses ?? 0,
            ProductCount = row.ProductCount ?? 0,
            NfeReceipt = string.IsNullOrEmpty(row.NfeReceipt) ? null : row.NfeReceipt,
            NfeProtocolResult = string.IsNullOrEmpty(row.NfeProtocolResult) ? null : row.NfeProtocolResult,
            NfeProtocol = string.IsNullOrEmpty(row.NfeProtocol) ? null : row.NfeProtocol,
            NfeKey = string.IsNullOrEmpty(row.NfeKey) ? null : row.NfeKey,
            PdfRelativePath = string.IsNullOrEmpty(row.NfeKey) ? null : Path.Combine(orderId.ToString(), "DFE" + row.NfeKey.Trim() + ".pdf"),
            XmlRelativePath = string.IsNullOrEmpty(row.NfeKey) ? null : Path.Combine(orderId.ToString(), "DFE" + row.NfeKey.Trim() + ".xml"),
            Receipt = row.Receipt,
            CfopList = cfopList
        };
    }

    public async Task<IReadOnlyList<CfopItemDto>> GetCfopListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PKId AS Id, CODE AS Code, DESCRIPTION AS Description FROM CFOP ORDER BY CODE";
        var rows = await _connection.QueryAsync<CfopRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(r => new CfopItemDto
        {
            Id = (byte)r.Id,
            Code = r.Code ?? "",
            Description = r.Description ?? "",
            DisplayText = $"{r.Code} - {r.Description}"
        }).ToList();
    }

    public async Task<IReadOnlyList<TransportSupplierDto>> GetTransportSuppliersAsync(CancellationToken cancellationToken = default)
    {
        var groupIdStr = _configuration["NFe:DeliverySupplierGroupId"];
        var groupId = int.TryParse(groupIdStr, out var g) ? g : 3;
        const string sql = @"
            SELECT Id, SocialName FROM (
                SELECT -1 AS Id, '0 - Selecione' AS SocialName, 0 AS SortKey
                UNION ALL
                SELECT PKId AS Id, SOCIAL_NAME AS SocialName, 1 AS SortKey
                FROM SUPPLIER
                WHERE SUPPLIER_GROUP_ID = @GroupId AND ACTIVE = 'Y'
            ) t ORDER BY SortKey, SocialName";
        var rows = await _connection.QueryAsync<TransportRow>(
            new CommandDefinition(sql, new { GroupId = groupId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(r => new TransportSupplierDto { Id = r.Id, SocialName = r.SocialName ?? "" }).ToList();
    }

    public async Task<IReadOnlyList<LastOrderForNfeDto>> GetLastPendingClosedOrdersForNfeAsync(int top = 30, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP (@Top)
                o.PKId AS OrderId,
                c.FANTASY_NAME AS ClientFantasyName,
                ci.NAME AS CityName,
                st.CODE AS StateCode,
                o.RECEIPT AS Receipt,
                o.NFE_RECEIPT AS NfeReceipt,
                o.NFE_PROTOCOL AS NfeProtocol,
                o.NFE_CANCEL_PROTOCOL AS NfeCancelProtocol
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
            LEFT JOIN [STATE] st ON st.PKId = c.ADDRESS_STATE_ID
            LEFT JOIN [CITY] ci ON ci.PKId = c.ADDRESS_CITY_ID
            WHERE o.STATUS = 'F' OR (o.STATUS = 'E' AND o.NFE_PROTOCOL_RESULT IS NULL)
            ORDER BY o.PKId DESC";
        var list = await _connection.QueryAsync<LastOrderRow>(
            new CommandDefinition(sql, new { Top = top }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.Select(r => new LastOrderForNfeDto
        {
            OrderId = r.OrderId,
            ClientFantasyName = r.ClientFantasyName ?? "",
            CityName = r.CityName,
            StateCode = r.StateCode,
            Receipt = r.Receipt,
            NfeReceipt = r.NfeReceipt,
            NfeProtocol = string.IsNullOrWhiteSpace(r.NfeProtocol) ? null : r.NfeProtocol.Trim(),
            NfeCancelProtocol = string.IsNullOrWhiteSpace(r.NfeCancelProtocol) ? null : r.NfeCancelProtocol.Trim()
        }).ToList();
    }

    public async Task<IReadOnlyList<LastEmittedNfeDto>> GetLastEmittedNfeListAsync(int top = 50, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP (@Top)
                o.PKId AS OrderId,
                o.RECEIPT AS Receipt,
                o.NFE_RECEIPT AS NfeReceipt,
                o.NFE_PROTOCOL AS NfeProtocol,
                o.NFE_CANCEL_PROTOCOL AS NfeCancelProtocol,
                o.NFE_KEY AS NfeKey,
                cli.SOCIAL_NAME AS SocialName
            FROM [ORDER] o
            INNER JOIN [CLIENT] cli ON cli.PKId = o.CLIENT_ID
            WHERE o.NFE_STATUS = 1
            ORDER BY o.PKId DESC";
        var list = await _connection.QueryAsync<LastEmittedNfeRow>(
            new CommandDefinition(sql, new { Top = top }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.Select(r => new LastEmittedNfeDto
        {
            OrderId = r.OrderId,
            Receipt = r.Receipt,
            NfeReceipt = string.IsNullOrWhiteSpace(r.NfeReceipt) ? null : r.NfeReceipt.Trim(),
            NfeProtocol = string.IsNullOrWhiteSpace(r.NfeProtocol) ? null : r.NfeProtocol.Trim(),
            NfeCancelProtocol = string.IsNullOrWhiteSpace(r.NfeCancelProtocol) ? null : r.NfeCancelProtocol.Trim(),
            SocialName = r.SocialName ?? "",
            NfeKey = string.IsNullOrWhiteSpace(r.NfeKey) ? null : r.NfeKey.Trim()
        }).ToList();
    }

    public async Task<OrderNfeDetailForDanfeDto?> GetOrderNfeDetailForDanfeAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                o.PKId AS OrderId,
                c.SOCIAL_NAME AS ClientName,
                o.NFE_KEY AS NfeKey,
                o.NFE_PROTOCOL AS NfeProtocol,
                o.NFE_CANCEL_PROTOCOL AS NfeCancelProtocol,
                o.NFE_RECEIPT AS NfeReceipt,
                o.RECEIPT AS Receipt
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
            WHERE o.PKId = @OrderId
              AND o.NFE_KEY IS NOT NULL
              AND LTRIM(RTRIM(ISNULL(o.NFE_KEY, ''))) <> ''
              AND (o.NFE_PROTOCOL IS NOT NULL OR o.NFE_CANCEL_PROTOCOL IS NOT NULL)";
        var row = await _connection.QuerySingleOrDefaultAsync<OrderNfeDetailForDanfeRow>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null || string.IsNullOrWhiteSpace(row.NfeKey))
            return null;
        var key = row.NfeKey.Trim();
        return new OrderNfeDetailForDanfeDto
        {
            OrderId = row.OrderId,
            ClientName = row.ClientName,
            NfeKey = key,
            NfeProtocol = string.IsNullOrWhiteSpace(row.NfeProtocol) ? null : row.NfeProtocol.Trim(),
            NfeCancelProtocol = string.IsNullOrWhiteSpace(row.NfeCancelProtocol) ? null : row.NfeCancelProtocol.Trim(),
            NfeReceipt = string.IsNullOrWhiteSpace(row.NfeReceipt) ? null : row.NfeReceipt.Trim(),
            Receipt = row.Receipt,
            PdfRelativePath = $"{orderId}/DFE{key}.pdf",
            XmlRelativePath = $"{orderId}/DFE{key}.xml"
        };
    }

    public async Task<IReadOnlyList<PendingOutboundNfeDto>> GetPendingOutboundNfeListAsync(int? orderId = null, int top = 15, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT TOP (@Top)
                o.PKId AS OrderId,
                o.RECEIPT AS Receipt,
                o.NFE_RECEIPT AS NfeReceipt,
                o.NFE_PROTOCOL AS NfeProtocol,
                o.NFE_CANCEL_PROTOCOL AS NfeCancelProtocol,
                o.NFE_KEY AS NfeKey,
                o.NFES_NO AS NfesNo,
                cli.SOCIAL_NAME AS SocialName
            FROM [ORDER] o
            INNER JOIN [CLIENT] cli ON cli.PKId = o.CLIENT_ID
            WHERE (o.NFE_STATUS IN (1) OR (o.NFES_NO IS NOT NULL AND LTRIM(RTRIM(o.NFES_NO)) <> ''))";
        if (orderId is > 0)
            sql += " AND o.PKId = @OrderId";
        sql += " ORDER BY o.PKId DESC";

        var list = await _connection.QueryAsync<PendingOutboundNfeRow>(
            new CommandDefinition(sql, new { Top = top, OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.Select(r => new PendingOutboundNfeDto
        {
            OrderId = r.OrderId,
            Receipt = r.Receipt,
            NfeReceipt = string.IsNullOrWhiteSpace(r.NfeReceipt) ? null : r.NfeReceipt.Trim(),
            NfeProtocol = string.IsNullOrWhiteSpace(r.NfeProtocol) ? null : r.NfeProtocol.Trim(),
            NfeCancelProtocol = string.IsNullOrWhiteSpace(r.NfeCancelProtocol) ? null : r.NfeCancelProtocol.Trim(),
            NfeKey = string.IsNullOrWhiteSpace(r.NfeKey) ? null : r.NfeKey.Trim(),
            NfesNo = string.IsNullOrWhiteSpace(r.NfesNo) ? null : r.NfesNo.Trim(),
            SocialName = r.SocialName ?? ""
        }).ToList();
    }

    public async Task<IReadOnlyList<PendingInboundNfeDto>> GetPendingInboundNfeListAsync(int? receiptNo = null, int top = 15, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT TOP (@Top)
                rid.RECEIPT_NO AS ReceiptNo,
                rid.INTERNAL_RECEIPT AS InternalReceipt,
                rid.NFE_RECEIPT AS NfeReceipt,
                rid.NFE_PROTOCOL AS NfeProtocol,
                rid.NFE_CANCEL_PROTOCOL AS NfeCancelProtocol,
                rid.NFE_KEY AS NfeKey,
                su.SOCIAL_NAME AS SocialName
            FROM [RECEIPT_IN_DATA] rid
            INNER JOIN [SUPPLIER] su ON su.PKId = rid.SUPPLIER_ID
            WHERE 1 = 1";
        if (receiptNo is > 0)
            sql += " AND rid.RECEIPT_NO = @ReceiptNo";
        sql += " ORDER BY rid.SYS_CREATION_DATE DESC";

        var list = await _connection.QueryAsync<PendingInboundNfeRow>(
            new CommandDefinition(sql, new { Top = top, ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.Select(r => new PendingInboundNfeDto
        {
            ReceiptNo = r.ReceiptNo,
            InternalReceipt = r.InternalReceipt,
            NfeReceipt = string.IsNullOrWhiteSpace(r.NfeReceipt) ? null : r.NfeReceipt.Trim(),
            NfeProtocol = string.IsNullOrWhiteSpace(r.NfeProtocol) ? null : r.NfeProtocol.Trim(),
            NfeCancelProtocol = string.IsNullOrWhiteSpace(r.NfeCancelProtocol) ? null : r.NfeCancelProtocol.Trim(),
            NfeKey = string.IsNullOrWhiteSpace(r.NfeKey) ? null : r.NfeKey.Trim(),
            SocialName = r.SocialName ?? ""
        }).ToList();
    }

    public async Task<ReceiptInNfeDetailForDanfeDto?> GetReceiptInNfeDetailForDanfeAsync(int receiptNo, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                rid.RECEIPT_NO AS ReceiptNo,
                su.SOCIAL_NAME AS SupplierName,
                rid.NFE_KEY AS NfeKey,
                rid.NFE_PROTOCOL AS NfeProtocol,
                rid.NFE_CANCEL_PROTOCOL AS NfeCancelProtocol,
                rid.NFE_RECEIPT AS NfeReceipt,
                rid.INTERNAL_RECEIPT AS InternalReceipt
            FROM [RECEIPT_IN_DATA] rid
            INNER JOIN [SUPPLIER] su ON su.PKId = rid.SUPPLIER_ID
            WHERE rid.RECEIPT_NO = @ReceiptNo
              AND rid.NFE_KEY IS NOT NULL
              AND LTRIM(RTRIM(ISNULL(rid.NFE_KEY, ''))) <> ''
              AND (rid.NFE_PROTOCOL IS NOT NULL OR rid.NFE_CANCEL_PROTOCOL IS NOT NULL)";
        var row = await _connection.QuerySingleOrDefaultAsync<ReceiptInNfeDetailRow>(
            new CommandDefinition(sql, new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null || string.IsNullOrWhiteSpace(row.NfeKey))
            return null;
        var key = row.NfeKey.Trim();
        var folder = "IN" + receiptNo;
        return new ReceiptInNfeDetailForDanfeDto
        {
            ReceiptNo = row.ReceiptNo,
            SupplierName = row.SupplierName,
            NfeKey = key,
            NfeProtocol = string.IsNullOrWhiteSpace(row.NfeProtocol) ? null : row.NfeProtocol.Trim(),
            NfeCancelProtocol = string.IsNullOrWhiteSpace(row.NfeCancelProtocol) ? null : row.NfeCancelProtocol.Trim(),
            NfeReceipt = string.IsNullOrWhiteSpace(row.NfeReceipt) ? null : row.NfeReceipt.Trim(),
            InternalReceipt = row.InternalReceipt,
            PdfRelativePath = $"{folder}/DFE{key}.pdf",
            XmlRelativePath = $"{folder}/DFE{key}.xml"
        };
    }

    public async Task<NfesPrintInfoDto?> GetNfesPrintInfoByNfesNoAsync(string nfesNo, CancellationToken cancellationToken = default)
    {
        var no = (nfesNo ?? "").Trim();
        if (string.IsNullOrEmpty(no))
            return null;

        const string sql = @"
            SELECT TOP 1
                o.PKId AS OrderId,
                o.NFES_NO AS NfesNo,
                o.NFES_CHECK_CODE AS NfesCheckCode,
                o.NFES_CHAVE_ACESSO AS NfesChaveAcesso,
                c.EMAIL AS ClientEmail,
                ISNULL(o.NFES_EMAIL_COUNT, 0) AS NfesEmailCount
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
            WHERE o.NFES_NO = @NfesNo
            ORDER BY o.SYS_CREATION_DATE DESC";
        var row = await _connection.QuerySingleOrDefaultAsync<NfesPrintInfoRow>(
            new CommandDefinition(sql, new { NfesNo = no }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null || string.IsNullOrWhiteSpace(row.NfesNo))
            return null;
        return new NfesPrintInfoDto
        {
            OrderId = row.OrderId,
            NfesNo = row.NfesNo.Trim(),
            NfesCheckCode = string.IsNullOrWhiteSpace(row.NfesCheckCode) ? null : row.NfesCheckCode.Trim(),
            NfesChaveAcesso = string.IsNullOrWhiteSpace(row.NfesChaveAcesso) ? null : row.NfesChaveAcesso.Trim(),
            ClientEmail = string.IsNullOrWhiteSpace(row.ClientEmail) ? null : row.ClientEmail.Trim(),
            NfesEmailCount = row.NfesEmailCount
        };
    }

    public async Task<EmitNfeResult> EmitNfeAsync(EmitNfeRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
        if (request.OrderId <= 0 || string.IsNullOrWhiteSpace(request.NfeNumber))
            return new EmitNfeResult { Success = false, Message = "Pedido e número da NFe são obrigatórios." };

        var orderInfo = await GetOrderInfoForNfeAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (orderInfo == null)
            return new EmitNfeResult { Success = false, Message = "Pedido inexistente ou não permitido para NFe (status F ou E, com produtos)." };
        if (!string.IsNullOrEmpty(orderInfo.NfeProtocol))
            return new EmitNfeResult { Success = false, Message = "Este pedido já possui NFe autorizada. Não é permitido criar nova NFe." };
        if (!string.IsNullOrEmpty(orderInfo.NfeProtocolResult) && orderInfo.NfeProtocolResult == "100")
            return new EmitNfeResult { Success = false, Message = "NOTA FISCAL JÁ EMITIDA PARA ESTE PEDIDO." };

        if (request.Discount.HasValue)
        {
            var discount = Math.Clamp(request.Discount.Value, 0, 99);
            await UpdateOrderDiscountAsync(request.OrderId, discount, cancellationToken).ConfigureAwait(false);
            if (discount != orderInfo.Discount)
            {
                orderInfo = await GetOrderInfoForNfeAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
                if (orderInfo == null)
                    return new EmitNfeResult { Success = false, Message = "Pedido inexistente ou não permitido para NFe (status F ou E, com produtos)." };
            }
        }

        var reprint = request.NfeNumber.TrimStart().StartsWith("R", StringComparison.OrdinalIgnoreCase);
        var nfeNumStr = request.NfeNumber.Trim().TrimStart('R', 'r');
        if (!int.TryParse(nfeNumStr, out var nfs) || nfs <= 0)
            return new EmitNfeResult { Success = false, Message = "Número da NFe inválido." };

        var emitCnpj = _configuration["NFe:EmitCnpj"];
        if (string.IsNullOrWhiteSpace(emitCnpj))
            return new EmitNfeResult { Success = false, Message = "Configure NFe:EmitCnpj (e demais NFe:Emit*) em appsettings para emissão." };
        emitCnpj = NfeChaveHelper.CleanDigits(emitCnpj);
        if (emitCnpj.Length != 14)
            return new EmitNfeResult { Success = false, Message = "NFe:EmitCnpj deve conter 14 dígitos." };

        var emit = GetEmitFromConfig();
        var dest = await GetDestFromOrderAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (dest == null)
            return new EmitNfeResult { Success = false, Message = "Dados do cliente não encontrados." };
        ApplyDestRazaoSocialForEnvironment(dest);

        var details = await GetOrderDetailsForNfeAsync(request.OrderId, request.CfopId, cancellationToken).ConfigureAwait(false);
        if (details.Count == 0)
            return new EmitNfeResult { Success = false, Message = "Nenhum item no pedido para emitir NFe." };

        var shipmentCost = 0m;

        // Desconto do pedido (igual ao legado): vNF = vProd - vDesc + vFrete + vOutro => vDesc = grossProduct + vFrete + vOutro - netTotal
        var grossProduct = details.Sum(d => d.VProd);
        var netTotal = orderInfo.OrderTotal - orderInfo.ShipmentCost;
        if (netTotal < 0) netTotal = 0;
        var otherExpenses = orderInfo.OtherExpenses;
        var discountAmount = Math.Round(grossProduct + shipmentCost + otherExpenses - netTotal, 2, MidpointRounding.AwayFromZero);
        if (discountAmount < 0) discountAmount = 0;
        DistributeDiscountToDetails(details, discountAmount, grossProduct);
        DistributeOutroToDetails(details, otherExpenses, grossProduct);

        try
        {
        var totalVnf = netTotal;
        var cnf8 = NfeChaveHelper.LeftZero((request.OrderId % 100000000).ToString(), 8);
        var chave = NfeChaveHelper.GetChaveAcesso(emitCnpj, DateTime.Now, nfs, int.Parse(cnf8, System.Globalization.CultureInfo.InvariantCulture), 1);
        var tpAmb = (_configuration["NFe:NfeEnvironment"] ?? "").Equals("test", StringComparison.OrdinalIgnoreCase) ? "2" : "1";
        var dhEmi = GetNfeDateTimeNow();

        var buildInput = new NfeBuildInput
        {
            Chave = chave,
            NfeNumber = nfs,
            OrderId = request.OrderId,
            Cnf8Digits = cnf8,
            DhEmi = dhEmi,
            NatOp = "VENDA",
            TpAmb = tpAmb,
            IdDest = (dest.Uf != "SP" && dest.Uf != "EX" ? "2" : "1"),
            VerProc = _configuration["NFe:VerProc"] ?? "1.0",
            Emit = emit,
            Dest = dest,
            Det = details,
            TotalVnf = totalVnf,
            TotalVfrete = shipmentCost,
            TotalVdesc = discountAmount,
            TotalVoutro = otherExpenses,
            ModFrete = request.FreightEmitenteDestinatario,
            VolEsp = request.PackageSpecies ?? "CAIXA",
            VolQty = request.PackageQuantity ?? "1",
            PesoB = request.WeightGross,
            PesoL = request.WeightNet,
            IcmsAliqPercent = ParseTaxPercent(_configuration["NFe:ICMS_ALIQ"] ?? "18"),
            PisAliqPercent = ParseTaxPercent(_configuration["NFe:PisAliq"] ?? "1.65"),
            CofinsAliqPercent = ParseTaxPercent(_configuration["NFe:CofinsAliq"] ?? "3"),
            InfCpl = await BuildInfCplAsync(request.OrderId, request.InformacoesComplementares, cancellationToken).ConfigureAwait(false)
        };

        XmlDocument nfeDoc;
        try
        {
            nfeDoc = _xmlBuilder.BuildNfeXml(buildInput);
            nfeDoc = _xmlSigner.SignNfeXml(nfeDoc, "NFe" + chave);
        }
        catch (Exception ex)
        {
            return new EmitNfeResult { Success = false, Message = "Erro ao montar/assinar NFe: " + ex.Message };
        }

        var signedXml = nfeDoc.OuterXml.Replace("utf-16", "utf-8");

        var xsdDir = _configuration["NFe:NfeXsdPath"];
        if (string.IsNullOrWhiteSpace(xsdDir))
            xsdDir = Path.Combine(AppContext.BaseDirectory, "Schemas");
        var validationErrors = _schemaValidator.ValidateXml(signedXml, xsdDir);
        if (validationErrors.Count > 0)
            return new EmitNfeResult { Success = false, Message = "Validação XSD: " + string.Join("; ", validationErrors) };

        await SaveXmlWithRetryAsync(request.OrderId, chave + "-nfe.xml", signedXml, cancellationToken).ConfigureAwait(false);

        // Montar enviNFe e chamar SEFAZ
        var enviNfeXml = BuildEnviNfeXml(signedXml, request.OrderId.ToString());
        XmlDocument retDoc;
        try
        {
            retDoc = await _sefazClient.NfeAutorizacaoLoteAsync(enviNfeXml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new EmitNfeResult { Success = false, Message = "Erro SEFAZ: " + ex.Message };
        }

        await SaveXmlWithRetryAsync(request.OrderId, chave + "-rec.xml", retDoc.OuterXml, cancellationToken).ConfigureAwait(false);

        // Interpretar igual ao legado (receiptSync.aspx.cs): retNfe.Item is TProtNFe -> prot.infProt.cStat e prot.infProt.xMotivo
        var root = retDoc.DocumentElement ?? retDoc.FirstChild as XmlElement;
        if (root == null)
            return new EmitNfeResult { Success = false, Message = "Resposta SEFAZ inválida." };

        var ns = new XmlNamespaceManager(retDoc.NameTable);
        ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");
        var cStat = GetChildText(root, "cStat");
        var xMotivo = GetChildText(root, "xMotivo");

        // 103 = Lote recebido, 104 = Lote processado (síncrono). Legado: quando cStat 104 o Item é protNFe.
        var protNFe = root.SelectSingleNode("./nfe:protNFe", ns) ?? root.SelectSingleNode("./*[local-name()='protNFe']");
        string? protCStat = null;
        string? protMotivo = null;
        string? nProt = null;

        if (protNFe != null)
        {
            var infProt = protNFe.SelectSingleNode("./nfe:infProt", ns) ?? protNFe.SelectSingleNode("./*[local-name()='infProt']");
            if (infProt != null)
            {
                protCStat = GetChildText(infProt, "cStat");
                protMotivo = GetChildText(infProt, "xMotivo");
                nProt = GetChildText(infProt, "nProt");
            }
        }

        // Erro de lote (cStat não 103/100/104)
        if (cStat != "103" && cStat != "100" && cStat != "104")
            return new EmitNfeResult { Success = false, Message = NormalizeSefazMessage($"Erro {cStat} - {xMotivo}") };

        // Modo síncrono (cStat 104): resultado real em prot.infProt. Legado: if (!prot.infProt.cStat.Equals("100")) retVal = "Erro " + prot.infProt.cStat + " - " + prot.infProt.xMotivo
        if (cStat == "104" && protNFe != null && protCStat != null)
        {
            if (protCStat != "100")
            {
                var msg = $"Erro {protCStat} - {protMotivo}";
                if (string.IsNullOrWhiteSpace(protMotivo) && !string.IsNullOrWhiteSpace(xMotivo))
                    msg = $"Erro {protCStat} - {xMotivo}";
                return new EmitNfeResult { Success = false, Message = NormalizeSefazMessage(msg) };
            }
        }
        else if (cStat == "104" && (protNFe == null || protCStat == null))
            return new EmitNfeResult { Success = false, Message = NormalizeSefazMessage(string.IsNullOrEmpty(xMotivo) ? "Resposta SEFAZ sem protocolo (cStat 104)." : $"Erro {cStat} - {xMotivo}") };

        if (protNFe == null)
            return new EmitNfeResult { Success = false, Message = NormalizeSefazMessage(string.IsNullOrEmpty(xMotivo) ? "Resposta SEFAZ sem protocolo." : $"Erro {cStat} - {xMotivo}") };

        if (protCStat != "100")
        {
            var msg = $"Erro {protCStat} - {protMotivo}";
            if (string.IsNullOrWhiteSpace(protMotivo) && !string.IsNullOrWhiteSpace(xMotivo))
                msg = $"Erro {protCStat} - {xMotivo}";
            return new EmitNfeResult { Success = false, Message = NormalizeSefazMessage(msg) };
        }

        await UpdateOrderNfeSuccessAsync(request.OrderId, chave, nProt ?? "", nfs, cancellationToken).ConfigureAwait(false);

        // RECEIPT row após NFE_KEY em [ORDER]: evita gatilhos/jobs (ex. TRANSACTION_LOG TRX_CODE=1 p/ e-mail da chave) enfileirarem com nfeKey null.
        if (!reprint)
            await InsertReceiptAsync(request.OrderId, nfs, request, (decimal)totalVnf, cancellationToken).ConfigureAwait(false);

        if (orderInfo.Status == "F")
            await SendOrderAfterNfeAsync(request.OrderId, cancellationToken).ConfigureAwait(false);

        var nfeProcXml = BuildNfeProcXml(signedXml, retDoc, protNFe);
        await SaveXmlWithRetryAsync(request.OrderId, "DFE" + chave + ".xml", nfeProcXml, cancellationToken).ConfigureAwait(false);

        var pdfPath = Path.Combine(_fileStorage.GetOrderDirectory(request.OrderId), "DFE" + chave + ".pdf");
        var nfeProcPath = Path.Combine(_fileStorage.GetOrderDirectory(request.OrderId), "DFE" + chave + ".xml");
        await _pdfGenerator.GeneratePdfAsync(request.OrderId, chave, nfeProcPath, pdfPath, cancellationToken).ConfigureAwait(false);

        if (!reprint)
            await SetReceiptNoAsync(nfs, cancellationToken).ConfigureAwait(false);

        var basePath = _configuration["NFe:NfeXmlPath"] ?? "";
        var pdfRelative = string.IsNullOrEmpty(basePath) ? null : Path.Combine(request.OrderId.ToString(), "DFE" + chave + ".pdf");
        var xmlRelative = Path.Combine(request.OrderId.ToString(), "DFE" + chave + ".xml");

        return new EmitNfeResult
        {
            Success = true,
            Message = $"NFe autorizada. Protocolo: {nProt}",
            NfeKey = chave,
            PdfRelativePath = pdfRelative,
            XmlRelativePath = xmlRelative
        };
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException != null)
                msg += " " + ex.InnerException.Message;
            return new EmitNfeResult { Success = false, Message = "Erro: " + msg };
        }
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException != null)
                msg += " " + ex.InnerException.Message;
            return new EmitNfeResult { Success = false, Message = "Erro: " + msg };
        }
    }

    public async Task<EmitNfeResult> RegeneratePdfOnlyAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT ISNULL(NFE_KEY, '') AS NfeKey FROM [ORDER] WHERE PKId = @OrderId";
        var chave = await _connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(chave) || chave.Length != 44)
            return new EmitNfeResult { Success = false, Message = "Pedido sem NFe emitida ou chave de acesso não encontrada." };

        var basePath = _configuration["NFe:NfeXmlPath"] ?? "";
        var orderDir = _fileStorage.GetOrderDirectory(orderId);
        var nfeProcPath = Path.Combine(orderDir, "DFE" + chave + ".xml");
        var pdfPath = Path.Combine(orderDir, "DFE" + chave + ".pdf");

        if (!File.Exists(nfeProcPath))
            return new EmitNfeResult { Success = false, Message = "Arquivo XML da NFe não encontrado. Não é possível gerar o PDF." };

        try
        {
            await _pdfGenerator.GeneratePdfAsync(orderId, chave, nfeProcPath, pdfPath, cancellationToken).ConfigureAwait(false);
            var pdfRelative = string.IsNullOrEmpty(basePath) ? null : Path.Combine(orderId.ToString(), "DFE" + chave + ".pdf");
            var xmlRelative = Path.Combine(orderId.ToString(), "DFE" + chave + ".xml");
            return new EmitNfeResult
            {
                Success = true,
                Message = "PDF gerado com sucesso.",
                NfeKey = chave,
                PdfRelativePath = pdfRelative,
                XmlRelativePath = xmlRelative
            };
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException != null) msg += " " + ex.InnerException.Message;
            return new EmitNfeResult { Success = false, Message = "Erro ao gerar PDF: " + msg };
        }
    }

    public async Task<NfeCancelInfoDto?> GetNfeCancelInfoByReceiptNoAsync(int receiptNo, CancellationToken cancellationToken = default)
    {
        const string sqlOrder = @"
            SELECT TOP 1 o.PKId AS OrderOrReceiptInId, o.NFE_KEY AS NfeKey, o.NFE_PROTOCOL AS NfeProtocol
            FROM [ORDER] o
            WHERE o.RECEIPT = @ReceiptNo AND o.NFE_PROTOCOL IS NOT NULL
            ORDER BY o.PKId DESC";
        var orderRow = await _connection.QuerySingleOrDefaultAsync<NfeCancelInfoRow>(
            new CommandDefinition(sqlOrder, new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (orderRow != null)
            return new NfeCancelInfoDto
            {
                IsSale = true,
                OrderOrReceiptInId = orderRow.OrderOrReceiptInId,
                NfeKey = orderRow.NfeKey ?? "",
                NfeProtocol = orderRow.NfeProtocol ?? "",
                OrderIdName = orderRow.OrderOrReceiptInId.ToString()
            };

        const string sqlReceiptIn = @"
            SELECT rid.RECEIPT_NO AS OrderOrReceiptInId, rid.NFE_KEY AS NfeKey, rid.NFE_PROTOCOL AS NfeProtocol
            FROM [RECEIPT_IN_DATA] rid
            WHERE rid.INTERNAL_RECEIPT = @ReceiptNo AND rid.NFE_PROTOCOL IS NOT NULL";
        var receiptInRow = await _connection.QuerySingleOrDefaultAsync<NfeCancelInfoRow>(
            new CommandDefinition(sqlReceiptIn, new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (receiptInRow == null)
            return null;

        return new NfeCancelInfoDto
        {
            IsSale = false,
            OrderOrReceiptInId = receiptInRow.OrderOrReceiptInId,
            NfeKey = receiptInRow.NfeKey ?? "",
            NfeProtocol = receiptInRow.NfeProtocol ?? "",
            OrderIdName = "IN" + receiptNo
        };
    }

    public async Task<IReadOnlyList<CanceledReceiptDto>> GetTodayCanceledReceiptsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        const string sql = @"
            SELECT RECEIPT_NO AS ReceiptNo, CANCEL_DATE AS CancelDate, MEMO AS Memo
            FROM [RECEIPT_CANCEL]
            WHERE CAST(SYS_CREATION_DATE AS DATE) = @Today
            ORDER BY SYS_CREATION_DATE DESC";
        var rows = await _connection.QueryAsync<CanceledReceiptRow>(
            new CommandDefinition(sql, new { Today = today }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(r => new CanceledReceiptDto
        {
            ReceiptNo = r.ReceiptNo,
            CancelDate = r.CancelDate,
            Memo = r.Memo
        }).ToList();
    }

    public async Task<byte> GetLastCcSeqNoAsync(int receiptNo, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP 1 SEQ_NO FROM [CC_NFE]
            WHERE RECEIPT_NO = @ReceiptNo
            ORDER BY SEQ_NO DESC";
        var seq = await _connection.ExecuteScalarAsync<byte?>(
            new CommandDefinition(sql, new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return seq ?? 0;
    }

    public async Task<IReadOnlyList<LastCcEventDto>> GetLastCcEventsAsync(int receiptNo, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP 20 cc.RECEIPT_NO AS ReceiptNo, cc.ORDER_ID AS OrderId, o.NFE_KEY AS NfeKey,
                cc.PROTOCOL AS Protocol, cc.REASON AS Reason, cc.SYS_CREATION_DATE AS SysCreationDate,
                cc.USER_ID AS UserId, c.EMAIL AS Email
            FROM [CC_NFE] cc
            INNER JOIN [ORDER] o ON o.PKId = cc.ORDER_ID AND o.RECEIPT = cc.RECEIPT_NO
            LEFT JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
            WHERE cc.RECEIPT_NO = @ReceiptNo
            ORDER BY cc.SEQ_NO DESC";
        var rows = await _connection.QueryAsync<LastCcEventRow>(
            new CommandDefinition(sql, new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(r => new LastCcEventDto
        {
            ReceiptNo = r.ReceiptNo,
            OrderId = r.OrderId,
            NfeKey = r.NfeKey ?? "",
            Protocol = r.Protocol,
            Reason = r.Reason ?? "",
            SysCreationDate = r.SysCreationDate,
            UserId = r.UserId ?? "",
            Email = r.Email
        }).ToList();
    }

    public async Task<IReadOnlyList<ReceiptReportRowDto>> GetReceiptReportAsync(DateTime firstDate, DateTime lastDate, CancellationToken cancellationToken = default)
    {
        var first = firstDate.Date;
        var last = lastDate.Date;
        if (last < first)
            last = first;

        const string sql = @"
            SELECT  r.RECEIPT_NO AS ReceiptNo,
                    ISNULL(cf.CODE, '') AS Cfop,
                    r.ORDER_ID AS OrderId,
                    CONVERT(varchar, r.SYS_CREATION_DATE, 103) AS RecDate,
                    CONVERT(varchar, r.SYS_CREATION_DATE, 108) AS RecTime,
                    r.TYPE AS Type,
                    r.NF_AMOUNT AS NfAmount,
                    CONVERT(varchar, rc.CANCEL_DATE, 103) AS CancelDate,
                    rc.USER_ID AS CancelUser,
                    rc.MEMO AS CancelMemo,
                    c.SOCIAL_NAME AS SocialName,
                    su.SOCIAL_NAME AS OtherName,
                    o.NFE_KEY AS NfeKey,
                    c.CNPJPF AS CnpjPf,
                    rid.NFE_KEY AS OnfeKey,
                    ISNULL(rid.INOUT, '') AS Inout
            FROM    [RECEIPT] r
            LEFT JOIN [CFOP] cf ON r.CFOP_ID = cf.PKId
            LEFT JOIN [RECEIPT_CANCEL] rc ON rc.RECEIPT_NO = r.RECEIPT_NO
            LEFT JOIN [ORDER] o ON o.PKId = r.ORDER_ID AND r.TYPE IN ('P', 'S')
            LEFT JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
            LEFT JOIN [RECEIPT_IN_DATA] rid ON rid.RECEIPT_NO = r.ORDER_ID AND r.TYPE = 'O'
            LEFT JOIN [SUPPLIER] su ON su.PKId = rid.SUPPLIER_ID
            WHERE   CAST(r.SYS_CREATION_DATE AS DATE) >= @FirstDate
            AND     CAST(r.SYS_CREATION_DATE AS DATE) <= @LastDate
            ORDER BY r.SYS_CREATION_DATE";
        var rows = await _connection.QueryAsync<ReceiptReportRowDto>(
            new CommandDefinition(sql, new { FirstDate = first, LastDate = last }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        var list = rows.AsList();
        if (list.Count == 0)
            return list;
        var seen = new HashSet<(int, string, string)>();
        var deduped = new List<ReceiptReportRowDto>(list.Count);
        foreach (var row in list)
        {
            var key = (row.ReceiptNo, row.Type ?? "", row.Inout ?? "");
            if (seen.Add(key))
                deduped.Add(row);
        }
        return deduped;
    }

    public Task<string> GenerateZipAsync(byte month, int year, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var nfePath = _configuration["NFe:NfeXmlPath"] ?? "";
        var downloadPath = _configuration["NFe:NfeXmlDownloadPath"] ?? "";
        if (string.IsNullOrWhiteSpace(nfePath))
            throw new InvalidOperationException("NFe:NfeXmlPath não configurado.");
        if (string.IsNullOrWhiteSpace(downloadPath))
            throw new InvalidOperationException("NFe:NfeXmlDownloadPath não configurado.");
        if (!Directory.Exists(nfePath))
            throw new InvalidOperationException("Pasta de NFe não existe: " + nfePath);
        Directory.CreateDirectory(downloadPath);
        var fileName = $"NFE_{month}_{year}.zip";
        var zipFullPath = Path.Combine(downloadPath, fileName);
        if (File.Exists(zipFullPath))
            File.Delete(zipFullPath);
        var dirs = Directory.GetDirectories(nfePath);
        var matchingDirs = dirs.Where(dir =>
        {
            var lastWrite = Directory.GetLastWriteTime(dir);
            return lastWrite.Month == month && lastWrite.Year == year;
        }).ToList();
        using (var zip = ZipFile.Open(zipFullPath, ZipArchiveMode.Create))
        {
            foreach (var dir in matchingDirs)
            {
                foreach (var file in Directory.GetFiles(dir, "DFE*.xml"))
                    zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Fastest);
                foreach (var file in Directory.GetFiles(dir, "DFE*.pdf"))
                    zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Fastest);
            }
        }
        return Task.FromResult(fileName);
    }

    public Task<IReadOnlyList<string>> ListZipFilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var downloadPath = _configuration["NFe:NfeXmlDownloadPath"] ?? "";
        if (string.IsNullOrWhiteSpace(downloadPath) || !Directory.Exists(downloadPath))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        var files = Directory.GetFiles(downloadPath, "*.zip");
        const int maxCount = 12;
        var names = files
            .Select(f => new { FullPath = f, Name = Path.GetFileName(f), LastWrite = File.GetLastWriteTimeUtc(f) })
            .Where(x => x.Name != null)
            .OrderByDescending(x => x.LastWrite)
            .Take(maxCount)
            .Select(x => x.Name!)
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(names);
    }

    private static readonly IReadOnlyDictionary<string, int> UfToCodIbge = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["AC"] = 12, ["AL"] = 27, ["AM"] = 13, ["AP"] = 16, ["BA"] = 29, ["CE"] = 23, ["DF"] = 53,
        ["ES"] = 32, ["GO"] = 52, ["MA"] = 21, ["MG"] = 31, ["MS"] = 50, ["MT"] = 51, ["PA"] = 15,
        ["PB"] = 25, ["PE"] = 26, ["PI"] = 22, ["PR"] = 41, ["RJ"] = 33, ["RN"] = 24, ["RO"] = 11,
        ["RR"] = 14, ["RS"] = 43, ["SC"] = 42, ["SE"] = 28, ["SP"] = 35, ["TO"] = 17
    };

    public async Task<ServiceStatusResultDto> GetServiceStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var siglaUf = (_configuration["NFe:Siglauf"] ?? "SP").Trim().ToUpperInvariant();
        if (siglaUf.Length != 2) siglaUf = "SP";
        if (!UfToCodIbge.TryGetValue(siglaUf, out var cUF))
            cUF = 35; // SP default
        var tpAmb = (_configuration["NFe:NfeEnvironment"] ?? "").Equals("test", StringComparison.OrdinalIgnoreCase) ? "2" : "1";
        var consStatServXml = $@"<consStatServ xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00""><tpAmb>{tpAmb}</tpAmb><cUF>{cUF}</cUF><xServ>STATUS</xServ></consStatServ>";

        var retDoc = await _sefazClient.NfeStatusServicoAsync(consStatServXml, cancellationToken).ConfigureAwait(false);
        var root = retDoc.DocumentElement;
        if (root == null)
            throw new InvalidOperationException("Resposta SEFAZ retConsStatServ vazia.");

        var versao = root.GetAttribute("versao");
        if (string.IsNullOrEmpty(versao))
            versao = GetChildText(root, "versao");

        return new ServiceStatusResultDto
        {
            CStat = GetChildText(root, "cStat"),
            XMotivo = GetChildText(root, "xMotivo"),
            Versao = versao,
            TpAmb = GetChildText(root, "tpAmb"),
            VerAplic = GetChildText(root, "verAplic"),
            CUf = GetChildText(root, "cUF"),
            DhRecbto = GetChildText(root, "dhRecbto"),
            TMed = GetChildText(root, "tMed"),
            DhRetorno = GetChildText(root, "dhRetorno"),
            XObs = GetChildText(root, "xObs")
        };
    }

    public async Task<SendCceResult> SendCceAsync(SendCceRequest request, CancellationToken cancellationToken = default)
    {
        var text = request.CorrectionText?.Trim() ?? "";
        if (text.Length < 15)
            return new SendCceResult { Success = false, Message = "A descrição da correção deve ter no mínimo 15 caracteres." };
        if (text.Length > 1000)
            return new SendCceResult { Success = false, Message = "A descrição da correção deve ter no máximo 1000 caracteres." };

        var info = await GetNfeCancelInfoByReceiptNoAsync(request.ReceiptNo, cancellationToken).ConfigureAwait(false);
        if (info == null)
            return new SendCceResult { Success = false, Message = "Nota não encontrada ou sem NFe autorizada." };
        if (!info.IsSale)
            return new SendCceResult { Success = false, Message = "Carta de correção disponível apenas para NFe de venda." };

        var lastSeq = await GetLastCcSeqNoAsync(request.ReceiptNo, cancellationToken).ConfigureAwait(false);
        var seqNo = (byte)(lastSeq + 1);
        if (seqNo < 1 || seqNo > 20)
            return new SendCceResult { Success = false, Message = "Número máximo de cartas de correção (20) já atingido para esta nota." };

        var userId = _configuration["NFe:CancelUserId"] ?? "SYS";
        var appId = _configuration["NFe:ApplicationId"] ?? "EUROERP";
        var emitCnpj = NfeChaveHelper.CleanDigits(_configuration["NFe:EmitCnpj"] ?? "");
        var tpAmb = (_configuration["NFe:NfeEnvironment"] ?? "").Equals("test", StringComparison.OrdinalIgnoreCase) ? "2" : "1";
        var dhEvento = GetNfeDateTimeNow();
        var nSeqStr = seqNo.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var evtId = "ID110110" + info.NfeKey + nSeqStr.PadLeft(2, '0');
        var xCorrecao = EscapeXml(text);
        if (xCorrecao.Length > 1000) xCorrecao = xCorrecao.Substring(0, 1000);
        const string xCondUso = "A Carta de Correcao e disciplinada pelo paragrafo 1o-A do art. 7o do Convenio S/N, de 15 de dezembro de 1970 e pode ser utilizada para regularizacao de erro ocorrido na emissao de documento fiscal, desde que o erro nao esteja relacionado com: I - as variaveis que determinam o valor do imposto tais como: base de calculo, aliquota, diferenca de preco, quantidade, valor da operacao ou da prestacao; II - a correcao de dados cadastrais que implique mudanca do remetente ou do destinatario; III - a data de emissao ou de saida.";

        var envEventoXml = $@"<envEvento versao=""1.00"" xmlns=""http://www.portalfiscal.inf.br/nfe"">
  <idLote>{request.ReceiptNo}</idLote>
  <evento versao=""1.00"">
    <infEvento Id=""{evtId}"">
      <cOrgao>35</cOrgao>
      <tpAmb>{tpAmb}</tpAmb>
      <CNPJ>{emitCnpj}</CNPJ>
      <chNFe>{info.NfeKey}</chNFe>
      <dhEvento>{dhEvento}</dhEvento>
      <tpEvento>110110</tpEvento>
      <nSeqEvento>{nSeqStr}</nSeqEvento>
      <verEvento>1.00</verEvento>
      <detEvento versao=""1.00"">
        <descEvento>Carta de Correcao</descEvento>
        <xCorrecao>{xCorrecao}</xCorrecao>
        <xCondUso>{EscapeXml(xCondUso)}</xCondUso>
      </detEvento>
    </infEvento>
  </evento>
</envEvento>";

        XmlDocument envDoc;
        try
        {
            envDoc = new XmlDocument();
            envDoc.LoadXml(envEventoXml);
            envDoc = _xmlSigner.SignEventXml(envDoc, evtId);
        }
        catch (Exception ex)
        {
            return new SendCceResult { Success = false, Message = "Erro ao assinar evento: " + ex.Message };
        }

        var signedXml = envDoc.OuterXml.Replace("utf-16", "utf-8");
        var xsdDir = _configuration["NFe:NfeXsdPath"];
        if (string.IsNullOrWhiteSpace(xsdDir))
            xsdDir = Path.Combine(AppContext.BaseDirectory, "Schemas");
        var validationErrors = _schemaValidator.ValidateEventoCceXml(signedXml, xsdDir);
        if (validationErrors.Count > 0)
            return new SendCceResult { Success = false, Message = "Validação XSD: " + string.Join("; ", validationErrors) };

        await _fileStorage.SaveXmlToFolderAsync(info.OrderIdName, info.NfeKey + "-ped-cce.xml", signedXml, cancellationToken).ConfigureAwait(false);

        XmlDocument retDoc;
        try
        {
            retDoc = await _sefazClient.NfeRecepcaoEventoAsync(signedXml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new SendCceResult
            {
                Success = false,
                Message = "Erro SEFAZ: " + ex.Message,
                SefazXMotivo = ex.Message
            };
        }

        var root = retDoc.DocumentElement ?? retDoc.FirstChild as XmlElement;
        if (root == null)
            return new SendCceResult { Success = false, Message = "Resposta SEFAZ inválida." };

        var cStat = GetChildText(root, "cStat");
        var xMotivo = GetChildText(root, "xMotivo");
        var retEvento = root.SelectSingleNode("*[local-name()='retEvento']") as XmlElement;
        string? evtCStat = null;
        string? evtXEvento = null;
        string? evtXMotivo = null;
        string? nProt = null;
        if (retEvento != null)
        {
            var infEvt = retEvento.SelectSingleNode("*[local-name()='infEvento']");
            if (infEvt != null)
            {
                evtCStat = GetChildText(infEvt, "cStat");
                evtXEvento = GetChildText(infEvt, "xEvento");
                evtXMotivo = GetChildText(infEvt, "xMotivo");
                nProt = GetChildText(infEvt, "nProt");
            }
        }

        if (cStat != "128" || evtCStat == null || (evtCStat != "135" && evtCStat != "136"))
        {
            return new SendCceResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(evtXMotivo) ? ($"Erro {evtCStat ?? cStat} - {xMotivo}") : ($"Erro {evtCStat ?? cStat} - {evtXMotivo}"),
                SefazCStat = evtCStat ?? cStat,
                SefazXMotivo = evtXMotivo ?? xMotivo,
                SefazXEvento = evtXEvento
            };
        }

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sqlInsert = @"
            INSERT INTO [CC_NFE] (RECEIPT_NO, ORDER_ID, REASON, SEQ_NO, PROTOCOL, NFE_STATUS, SYS_CREATION_DATE, USER_ID, APPLICATION_ID, EMAIL_COUNT)
            VALUES (@ReceiptNo, @OrderId, @Reason, @SeqNo, @Protocol, NULL, GETDATE(), @UserId, @AppId, 0)";
        await _connection.ExecuteAsync(new CommandDefinition(sqlInsert, new
        {
            ReceiptNo = request.ReceiptNo,
            OrderId = info.OrderOrReceiptInId,
            Reason = text,
            SeqNo = seqNo,
            Protocol = nProt ?? "",
            UserId = userId,
            AppId = appId
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        await _fileStorage.SaveXmlToFolderAsync(info.OrderIdName, info.NfeKey + "-cce.xml", retDoc.OuterXml, cancellationToken).ConfigureAwait(false);

        return new SendCceResult
        {
            Success = true,
            Message = "Carta de correção registrada na SEFAZ.",
            Protocol = nProt,
            SefazCStat = evtCStat,
            SefazXEvento = evtXEvento,
            SefazXMotivo = evtXMotivo
        };
    }

    public async Task<CancelNfeResult> CancelNfeAsync(CancelNfeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Justification) || request.Justification.Length < 15 || request.Justification.Length > 255)
            return new CancelNfeResult { Success = false, Message = "O motivo deve ter entre 15 e 255 caracteres." };

        var info = await GetNfeCancelInfoByReceiptNoAsync(request.ReceiptNo, cancellationToken).ConfigureAwait(false);
        if (info == null)
            return new CancelNfeResult { Success = false, Message = "Nota não encontrada ou sem NFe autorizada." };

        const string sqlExists = "SELECT 1 FROM [RECEIPT_CANCEL] WHERE RECEIPT_NO = @ReceiptNo";
        var alreadyCanceled = await _connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(sqlExists, new { request.ReceiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (alreadyCanceled.HasValue && alreadyCanceled.Value == 1)
            return new CancelNfeResult { Success = false, Message = $"Não é possível cancelar. A Nota {request.ReceiptNo} já foi cancelada!" };

        var userId = _configuration["NFe:CancelUserId"] ?? "SYS";
        var appId = _configuration["NFe:ApplicationId"] ?? "EUROERP";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using var trx = _connection.BeginTransaction();
        try
        {
            const string sqlInsert = @"
                INSERT INTO [RECEIPT_CANCEL] (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, CANCEL_DATE, RECEIPT_NO, MEMO, RECEIPT_FORM)
                VALUES (GETDATE(), @UserId, @AppId, @CancelDate, @ReceiptNo, @Memo, @ReceiptForm)";
            await _connection.ExecuteAsync(new CommandDefinition(sqlInsert, new
            {
                UserId = userId,
                AppId = appId,
                CancelDate = request.CancelDate,
                ReceiptNo = request.ReceiptNo,
                Memo = request.Justification.Trim(),
                ReceiptForm = request.ReceiptNo
            }, cancellationToken: cancellationToken, transaction: trx)).ConfigureAwait(false);

            var emitCnpj = NfeChaveHelper.CleanDigits(_configuration["NFe:EmitCnpj"] ?? "");
            var tpAmb = (_configuration["NFe:NfeEnvironment"] ?? "").Equals("test", StringComparison.OrdinalIgnoreCase) ? "2" : "1";
            // dhEvento = data/hora atual no fuso do emitente (NFe:GmtOffset), para não ser menor que a data de emissão da NFe (rejeição 577).
            var dhEvento = GetNfeDateTimeNow();
            var evtId = "ID110111" + info.NfeKey + "01";
            var xJust = EscapeXml(request.Justification.Trim());
            if (xJust.Length > 255) xJust = xJust.Substring(0, 255);

            var envEventoXml = $@"<envEvento versao=""1.00"" xmlns=""http://www.portalfiscal.inf.br/nfe"">
  <idLote>{request.ReceiptNo}</idLote>
  <evento versao=""1.00"">
    <infEvento Id=""{evtId}"">
      <cOrgao>35</cOrgao>
      <tpAmb>{tpAmb}</tpAmb>
      <CNPJ>{emitCnpj}</CNPJ>
      <chNFe>{info.NfeKey}</chNFe>
      <dhEvento>{dhEvento}</dhEvento>
      <tpEvento>110111</tpEvento>
      <nSeqEvento>1</nSeqEvento>
      <verEvento>1.00</verEvento>
      <detEvento versao=""1.00"">
        <descEvento>Cancelamento</descEvento>
        <nProt>{info.NfeProtocol}</nProt>
        <xJust>{xJust}</xJust>
      </detEvento>
    </infEvento>
  </evento>
</envEvento>";

            XmlDocument envDoc;
            try
            {
                envDoc = new XmlDocument();
                envDoc.LoadXml(envEventoXml);
                envDoc = _xmlSigner.SignEventXml(envDoc, evtId);
            }
            catch (Exception ex)
            {
                trx.Rollback();
                return new CancelNfeResult { Success = false, Message = "Erro ao assinar evento: " + ex.Message };
            }

            var signedXml = envDoc.OuterXml.Replace("utf-16", "utf-8");
            var xsdDir = _configuration["NFe:NfeXsdPath"];
            if (string.IsNullOrWhiteSpace(xsdDir))
                xsdDir = Path.Combine(AppContext.BaseDirectory, "Schemas");
            var validationErrors = _schemaValidator.ValidateEventoCancelamentoXml(signedXml, xsdDir);
            if (validationErrors.Count > 0)
            {
                trx.Rollback();
                return new CancelNfeResult { Success = false, Message = "Validação XSD: " + string.Join("; ", validationErrors) };
            }

            await _fileStorage.SaveXmlToFolderAsync(info.OrderIdName, info.NfeKey + "-ped-can.xml", signedXml, cancellationToken).ConfigureAwait(false);

            XmlDocument retDoc;
            try
            {
                retDoc = await _sefazClient.NfeRecepcaoEventoAsync(signedXml, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                trx.Rollback();
                return new CancelNfeResult
                {
                    Success = false,
                    Message = "Erro SEFAZ: " + ex.Message,
                    SefazXMotivo = ex.Message
                };
            }

            var root = retDoc.DocumentElement ?? retDoc.FirstChild as XmlElement;
            if (root == null)
            {
                trx.Rollback();
                return new CancelNfeResult { Success = false, Message = "Resposta SEFAZ inválida." };
            }

            var cStat = GetChildText(root, "cStat");
            var xMotivo = GetChildText(root, "xMotivo");
            var retEvento = root.SelectSingleNode("*[local-name()='retEvento']") as XmlElement;
            string? evtCStat = null;
            string? evtXEvento = null;
            string? evtXMotivo = null;
            string? nProt = null;
            if (retEvento != null)
            {
                var infEvt = retEvento.SelectSingleNode("*[local-name()='infEvento']");
                if (infEvt != null)
                {
                    evtCStat = GetChildText(infEvt, "cStat");
                    evtXEvento = GetChildText(infEvt, "xEvento");
                    evtXMotivo = GetChildText(infEvt, "xMotivo");
                    nProt = GetChildText(infEvt, "nProt");
                }
            }

            if (cStat != "128" || evtCStat == null || (evtCStat != "135" && evtCStat != "136"))
            {
                trx.Rollback();
                return new CancelNfeResult
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(evtXMotivo) ? ($"Erro {evtCStat ?? cStat} - {xMotivo}") : ($"Erro {evtCStat ?? cStat} - {evtXMotivo}"),
                    SefazCStat = evtCStat ?? cStat,
                    SefazXMotivo = evtXMotivo ?? xMotivo,
                    SefazXEvento = evtXEvento
                };
            }

            if (info.IsSale)
            {
                const string sqlUpdateOrder = @"UPDATE [ORDER] SET NFE_CANCEL_PROTOCOL = @NProt, NFE_RECEIPT = NULL, NFE_PROTOCOL = NULL, NFE_EMAIL_COUNT = NULL, DELIVERY_SUPPLIER_ID = NULL, NFE_PROTOCOL_RESULT = NULL WHERE PKId = @Id";
                await _connection.ExecuteAsync(new CommandDefinition(sqlUpdateOrder, new { NProt = nProt ?? "", Id = info.OrderOrReceiptInId }, cancellationToken: cancellationToken, transaction: trx)).ConfigureAwait(false);
            }
            else
            {
                const string sqlUpdateReceiptIn = @"UPDATE [RECEIPT_IN_DATA] SET NFE_CANCEL_PROTOCOL = @NProt, NFE_RECEIPT = NULL, NFE_PROTOCOL = NULL, NFE_PROTOCOL_RESULT = NULL WHERE RECEIPT_NO = @Id";
                await _connection.ExecuteAsync(new CommandDefinition(sqlUpdateReceiptIn, new { NProt = nProt ?? "", Id = info.OrderOrReceiptInId }, cancellationToken: cancellationToken, transaction: trx)).ConfigureAwait(false);
            }

            trx.Commit();
            return new CancelNfeResult
            {
                Success = true,
                Message = "Cancelamento registrado na SEFAZ.",
                CancelProtocol = nProt,
                SefazCStat = evtCStat,
                SefazXEvento = evtXEvento,
                SefazXMotivo = evtXMotivo
            };
        }
        catch (Exception ex)
        {
            trx.Rollback();
            var msg = ex.Message;
            if (ex.InnerException != null) msg += " " + ex.InnerException.Message;
            return new CancelNfeResult { Success = false, Message = "Erro: " + msg };
        }
    }

    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static string GetChildText(XmlNode parent, string localName)
    {
        if (parent == null) return "";
        var child = parent.SelectSingleNode($"*[local-name()='{localName}']");
        return child?.InnerText?.Trim() ?? "";
    }

    /// <summary>Garante que a mensagem de erro SEFAZ nunca fique vazia para ser exibida na tela.</summary>
    private static string NormalizeSefazMessage(string? message)
    {
        var m = message?.Trim();
        return string.IsNullOrEmpty(m) ? "Erro SEFAZ (mensagem não disponível)." : m;
    }

    private async Task SaveXmlWithRetryAsync(int orderId, string fileName, string xmlContent, CancellationToken cancellationToken, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await _fileStorage.SaveXmlAsync(orderId, fileName, xmlContent, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string BuildEnviNfeXml(string signedNfeXml, string idLote)
    {
        var inner = signedNfeXml.Trim();
        if (inner.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var end = inner.IndexOf("?>", StringComparison.Ordinal);
            if (end >= 0) inner = inner.Substring(end + 2).Trim();
        }
        return $"<enviNFe xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\"><idLote>{idLote}</idLote><indSinc>1</indSinc>{inner}</enviNFe>";
    }

    private static string BuildNfeProcXml(string signedNfeXml, XmlDocument retDoc, XmlNode protNFe)
    {
        var inner = signedNfeXml.Trim();
        if (inner.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var end = inner.IndexOf("?>", StringComparison.Ordinal);
            if (end >= 0) inner = inner.Substring(end + 2).Trim();
        }
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\">" + inner + protNFe.OuterXml + "</nfeProc>";
    }

    private void ApplyDestRazaoSocialForEnvironment(NfeDestInput dest)
    {
        var destRazao = (_configuration["NFe:DestRazaoSocial"] ?? "").Trim();
        if (!string.IsNullOrEmpty(destRazao))
        {
            dest.Nome = destRazao;
            return;
        }
        var isHomolog = (_configuration["NFe:NfeEnvironment"] ?? "").Equals("test", StringComparison.OrdinalIgnoreCase);
        if (isHomolog)
            dest.Nome = HomologDestRazaoSocial;
    }

    private NfeEmitInput GetEmitFromConfig()
    {
        return new NfeEmitInput
        {
            Cnpj = _configuration["NFe:EmitCnpj"] ?? "",
            RazaoSocial = _configuration["NFe:EmitRazaoSocial"] ?? "",
            Fantasia = _configuration["NFe:EmitFantasia"] ?? "",
            Logradouro = _configuration["NFe:EmitLogradouro"] ?? "",
            Numero = _configuration["NFe:EmitNumero"] ?? "",
            Complemento = _configuration["NFe:EmitComplemento"],
            Bairro = _configuration["NFe:EmitBairro"] ?? "",
            CodigoMun = _configuration["NFe:EmitCodigoMun"] ?? "",
            Municipio = _configuration["NFe:EmitMunicipio"] ?? "",
            Uf = _configuration["NFe:Siglauf"] ?? "SP",
            Cep = _configuration["NFe:EmitCep"] ?? "",
            Fone = _configuration["NFe:EmitTelefone"],
            Ie = NfeChaveHelper.CleanIe((_configuration["NFe:EmitIE"] ?? "").Trim()),
            Crt = _configuration["NFe:Crt"] ?? "1"
        };
    }

    private async Task<NfeDestInput?> GetDestFromOrderAsync(int orderId, CancellationToken ct)
    {
        const string sql = @"
            SELECT c.PERSON_TYPE AS PersonType, c.CNPJPF AS CpfCnpj, c.SOCIAL_NAME AS Nome,
                c.ADDRESS_STREET AS Logradouro, c.ADDRESS_NUMBER AS Numero, c.ADDRESS_COMPLEMENT AS Complemento,
                c.ADDRESS_BLOCK AS Bairro, ci.C_MUN AS CodigoMun, ci.NAME AS Municipio, st.CODE AS Uf,
                c.ADDRESS_ZIPCODE AS Cep, c.PHONE1 AS Fone, c.EMAIL AS Email,
                ISNULL(c.STATE_INSCR,'') AS Ie
            FROM [ORDER] o
            INNER JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
            LEFT JOIN [CITY] ci ON ci.PKId = c.ADDRESS_CITY_ID
            LEFT JOIN [STATE] st ON st.PKId = c.ADDRESS_STATE_ID
            WHERE o.PKId = @OrderId";
        var row = await _connection.QuerySingleOrDefaultAsync<DestRow>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: ct)).ConfigureAwait(false);
        if (row == null) return null;
        var ie = NfeChaveHelper.CleanIe((row.Ie ?? "").Trim().ToUpperInvariant());
        var personType = (row.PersonType ?? "J").ToUpperInvariant();
        var iEExempt = ie == "ISENTO" || ie.Length < 3;
        string indIe;
        if (iEExempt && personType == "F")
            indIe = "9"; // Não contribuinte (PF isenta)
        else if (iEExempt && personType == "J")
            indIe = "2"; // Isento (PJ isenta)
        else
            indIe = "1"; // Contribuinte ICMS
        return new NfeDestInput
        {
            IsCpf = (row.PersonType ?? "J") == "F",
            CpfCnpj = NfeChaveHelper.CleanDigits(row.CpfCnpj ?? ""),
            Nome = row.Nome ?? "",
            Logradouro = row.Logradouro ?? "",
            Numero = row.Numero ?? "",
            Complemento = row.Complemento,
            Bairro = row.Bairro ?? "",
            CodigoMun = row.CodigoMun ?? "0000000",
            Municipio = row.Municipio ?? "",
            Uf = row.Uf ?? "",
            Cep = NfeChaveHelper.CleanDigits(row.Cep ?? ""),
            Fone = row.Fone,
            Email = row.Email,
            IndIeDest = indIe,
            Ie = indIe == "1" ? ie : null
        };
    }

    private async Task<List<NfeDetInput>> GetOrderDetailsForNfeAsync(int orderId, byte cfopId, CancellationToken ct)
    {
        var cfopCode = "5102";
        if (cfopId > 0)
        {
            var cfopRow = await _connection.QuerySingleOrDefaultAsync<CfopRow>(
                new CommandDefinition("SELECT PKId AS Id, CODE AS Code, DESCRIPTION AS Description FROM CFOP WHERE PKId = @Id", new { Id = cfopId }, cancellationToken: ct)).ConfigureAwait(false);
            if (cfopRow?.Code != null) cfopCode = cfopRow.Code.Replace(".", "");
        }
        else
        {
            var firstCfop = await _connection.ExecuteScalarAsync<string>(
                new CommandDefinition(
                    "SELECT TOP 1 cf.CODE FROM [ORDER_DETAILS] od JOIN [ORDER] o ON o.PKId = od.ORDER_ID JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID JOIN [PRODUCT] p ON p.PKId = od.PRODUCT_ID JOIN [CST_CFOP] csf ON csf.CSTB_ID = p.CSTB_ID AND csf.STATE_ID = c.ADDRESS_STATE_ID JOIN [CFOP] cf ON csf.CFOP_ID = cf.PKId WHERE od.ORDER_ID = @OrderId AND od.QUANTITY > 0",
                    new { OrderId = orderId }, cancellationToken: ct)).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(firstCfop)) cfopCode = firstCfop.Replace(".", "");
        }

        const string sql = @"
            SELECT od.QUANTITY AS Quantity,
                ROUND(od.PRICE * od.CONVERSION, 2) * (1 - od.DISCOUNT/100) AS UnitPrice,
                ROUND(ROUND(od.PRICE * od.CONVERSION, 2) * (1 - od.DISCOUNT/100), 2) * od.QUANTITY AS LineTotal,
                (CAST(ISNULL(pg.PRODUCT_CLASS_ID, 0) AS VARCHAR) + RIGHT('000000' + CAST(od.PRODUCT_ID AS VARCHAR), 7)) AS ProductCode,
                ISNULL(NULLIF(LTRIM(RTRIM(p.NAME)), ''), ISNULL(NULLIF(LTRIM(RTRIM(p.DESCRIPTION)), ''), 'Produto')) AS ProductName,
                ISNULL(fc.VALUE, '00000000') AS Ncm,
                ISNULL(p.CSTB_ID, '00') AS CstbId
            FROM [ORDER_DETAILS] od
            JOIN [PRODUCT] p ON p.PKId = od.PRODUCT_ID
            LEFT JOIN [PRODUCT_GROUP] pg ON pg.PKId = p.GROUP_ID
            LEFT JOIN [FISCAL_CLASS] fc ON fc.PKId = p.FISCAL_CLASS_ID
            WHERE od.ORDER_ID = @OrderId AND od.QUANTITY > 0
            ORDER BY od.ORDER_ID, od.PRODUCT_ID";
        var rows = await _connection.QueryAsync<OrderDetailRow>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: ct)).ConfigureAwait(false);
        var list = new List<NfeDetInput>();
        var n = 1;
        foreach (var r in rows)
        {
            var qty = r.Quantity;
            var unitPrice = r.UnitPrice;
            var lineTotal = r.LineTotal;
            var csosn = MapCstbToCsosn(r.CstbId);
            var xProd = ResolveProductDescription(r.ProductName);
            list.Add(new NfeDetInput
            {
                NItem = n++,
                CProd = (r.ProductCode ?? "").Trim(),
                XProd = xProd,
                Ncm = NfeChaveHelper.CleanDigits(r.Ncm ?? "00000000").PadRight(8, '0').Substring(0, 8),
                Cfop = cfopCode,
                QCom = qty,
                VUnCom = unitPrice,
                VProd = lineTotal,
                VDesc = 0,
                VFrete = 0,
                VOutro = 0,
                Csosn = csosn,
                Origem = "0"
            });
        }
        return list;
    }

    private static string MapCstbToCsosn(string? cstbId)
    {
        if (int.TryParse(cstbId?.Trim(), out var id))
            return MapCstbToCsosn(id);
        return "102";
    }

    private static string MapCstbToCsosn(int cstbId)
    {
        return cstbId switch { 101 => "101", 102 => "102", 103 => "103", 202 => "202", 500 => "500", 900 => "900", _ => "102" };
    }

    private static string ResolveProductDescription(string? productName)
    {
        var name = (productName ?? "Produto").Trim();
        return name.Length > 120 ? name[..120] : (name.Length > 0 ? name : "Produto");
    }

    private static decimal ParseTaxPercent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        var s = raw.Trim().Replace(',', '.');
        if (!decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return 0;
        if (d > 0 && d < 1)
            d *= 100;
        return d;
    }

    private async Task<string?> BuildInfCplAsync(int orderId, string? userInfo, CancellationToken ct)
    {
        const string sql = @"
            SELECT ISNULL(o.CAR_PROBLEM, '') AS CarProblem,
                   ISNULL(car.PLATE, '') AS CarPlate,
                   ISNULL(car.DESCRIPTION, '') AS CarDesc
            FROM [ORDER] o
            LEFT JOIN CAR car ON car.PKId = o.CAR_ID
            WHERE o.PKId = @OrderId";
        var row = await _connection.QuerySingleOrDefaultAsync<CarInfoRow>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: ct)).ConfigureAwait(false);
        var parts = new List<string>();
        var carLine = $"OS {orderId}";
        if (row != null)
        {
            var plate = (row.CarPlate ?? "").Trim();
            var desc = (row.CarDesc ?? "").Trim();
            if (plate.Length > 0)
                carLine += $" - {desc} Placa: {plate}";
            else if (desc.Length > 0)
                carLine += $" - {desc}";
            if (!string.IsNullOrWhiteSpace(row.CarProblem))
                parts.Add(row.CarProblem.Trim());
        }
        parts.Insert(0, carLine);
        if (!string.IsNullOrWhiteSpace(userInfo))
            parts.Add(userInfo.Trim());
        var text = string.Join("\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private sealed class CarInfoRow
    {
        public string? CarProblem { get; set; }
        public string? CarPlate { get; set; }
        public string? CarDesc { get; set; }
    }

    /// <summary>Distribui o desconto total (pedido) pelos itens proporcionalmente ao VProd. Último item recebe o ajuste de arredondamento.</summary>
    private static void DistributeDiscountToDetails(List<NfeDetInput> details, decimal discountAmount, decimal grossProduct)
    {
        if (details.Count == 0 || discountAmount <= 0 || grossProduct <= 0) return;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        decimal assigned = 0;
        for (var i = 0; i < details.Count; i++)
        {
            var det = details[i];
            decimal itemDesc;
            if (i < details.Count - 1 && grossProduct > 0)
            {
                itemDesc = Math.Round(discountAmount * det.VProd / grossProduct, 2, MidpointRounding.AwayFromZero);
                assigned += itemDesc;
            }
            else
                itemDesc = Math.Round(discountAmount - assigned, 2, MidpointRounding.AwayFromZero);
            det.VDesc = Math.Max(0, itemDesc);
        }
    }

    /// <summary>Distribui outras despesas (vOutro) pelos itens proporcionalmente ao VProd. Último item recebe o ajuste de arredondamento (igual ao legado).</summary>
    private static void DistributeOutroToDetails(List<NfeDetInput> details, decimal otherExpenses, decimal grossProduct)
    {
        if (details.Count == 0 || otherExpenses <= 0 || grossProduct <= 0) return;
        decimal assigned = 0;
        for (var i = 0; i < details.Count; i++)
        {
            var det = details[i];
            decimal itemOutro;
            if (i < details.Count - 1)
            {
                itemOutro = Math.Round(otherExpenses * det.VProd / grossProduct, 2, MidpointRounding.AwayFromZero);
                assigned += itemOutro;
            }
            else
                itemOutro = Math.Round(otherExpenses - assigned, 2, MidpointRounding.AwayFromZero);
            det.VOutro = Math.Max(0, itemOutro);
        }
    }

    /// <summary>Insere linha em [RECEIPT]. Chamado só após autorização SEFAZ e <see cref="UpdateOrderNfeSuccessAsync"/> (primeira emissão), para não disparar integrações que leem [ORDER].NFE_KEY no INSERT.</summary>
    private async Task InsertReceiptAsync(int orderId, int receiptNo, EmitNfeRequest request, decimal nfAmount, CancellationToken ct)
    {
        const string sql = @"INSERT INTO [RECEIPT] (RECEIPT_NO, RECEIPT_FORM_NO, ORDER_ID, SHIPMENT, DELIVERY_SUPPLIER_ID, MSG_ID, CFOP_ID, TYPE, NF_AMOUNT, SYS_CREATION_DATE, CATEGORY)
            VALUES (@ReceiptNo, 0, @OrderId, @Shipment, @DeliveryId, 0, @CfopId, 'P', @NfAmount, GETDATE(), 100)";
        var deliveryId = request.TransportSupplierId.HasValue && request.TransportSupplierId.Value > 0 ? request.TransportSupplierId.Value : 0;
        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            ReceiptNo = receiptNo,
            OrderId = orderId,
            Shipment = request.FreightEmitenteDestinatario,
            DeliveryId = deliveryId,
            CfopId = request.CfopId,
            NfAmount = nfAmount
        }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private async Task UpdateOrderNfeSuccessAsync(int orderId, string chave, string nProt, int receiptNo, CancellationToken ct)
    {
        const string sql = @"UPDATE [ORDER] SET RECEIPT = @ReceiptNo, NFE_RECEIPT = '', NFE_KEY = @Chave, NFE_STATUS = 1, NFE_PROTOCOL = @NProt, NFE_PROTOCOL_RESULT = '100', NFE_CANCEL_PROTOCOL = NULL
            WHERE PKId = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { OrderId = orderId, ReceiptNo = receiptNo, Chave = chave, NProt = nProt }, cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <summary>Marca o pedido como enviado (STATUS='E', SENT_DATE) após NFe emitida, igual ao legado sendOrder.</summary>
    private async Task SendOrderAfterNfeAsync(int orderId, CancellationToken ct)
    {
        const string sql = @"UPDATE [ORDER] SET STATUS = 'E', LAST_ACTV = 'SEND', STATUS_CHG_DATE = GETDATE(), SENT_DATE = GETDATE() WHERE PKId = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private async Task SetReceiptNoAsync(int currentReceiptNo, CancellationToken ct)
    {
        const string sql = "UPDATE SYS_CONTROL SET VALUE = @Value WHERE CODE = 'RECEIPT_NO'";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { Value = (currentReceiptNo + 1).ToString() }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private async Task UpdateOrderDiscountAsync(int orderId, decimal discount, CancellationToken ct)
    {
        const string sql = "UPDATE [ORDER] SET DISCOUNT = @Discount WHERE PKId = @OrderId";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { OrderId = orderId, Discount = discount }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private async Task<decimal> GetOrderTotalAsync(int orderId, CancellationToken ct)
    {
        const string sql = @"
            SELECT (SUM(t1.TOTAL) - MAX(t1.CREDIT)) * (1 - MAX(t1.DISC) / 100) + MAX(t1.OE) + MAX(t1.SHIP) AS CONVERTED_TOTAL_NET_PRICE
            FROM (
                SELECT CAST(SUM(ROUND(ROUND(ROUND(price * conversion, 2) * (1 - od.discount/100), 2) * quantity, 2)) AS DECIMAL(14,2)) AS TOTAL,
                    ISNULL(o.CREDIT, 0) AS CREDIT, ISNULL(o.OTHER_EXPENSES, 0) AS OE,
                    (ISNULL(od.ignore_order_disc, 0) - 1) * -1 * ISNULL(o.DISCOUNT, 0) AS DISC,
                    ISNULL(o.SHIPMENT_COST, 0) AS SHIP
                FROM [ORDER_DETAILS] od
                JOIN [ORDER] o ON o.PKId = od.ORDER_ID
                WHERE od.ORDER_ID = @OrderId AND od.QUANTITY > 0
                GROUP BY o.CREDIT, o.DISCOUNT, o.OTHER_EXPENSES, o.SHIPMENT_COST, od.IGNORE_ORDER_DISC
            ) t1";
        var total = await _connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: ct)).ConfigureAwait(false);
        return total ?? 0;
    }

    private async Task<IReadOnlyList<CfopItemDto>> GetOrderCfopListAsync(int orderId, CancellationToken ct)
    {
        const string sql = @"
            SELECT DISTINCT cf.PKId AS Id, cf.CODE AS Code, cf.DESCRIPTION AS Description
            FROM [ORDER_DETAILS] od
            JOIN [ORDER] o ON o.PKId = od.ORDER_ID
            JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
            JOIN [PRODUCT] p ON p.PKId = od.PRODUCT_ID
            JOIN [CST_CFOP] csf ON csf.CSTB_ID = p.CSTB_ID AND csf.STATE_ID = c.ADDRESS_STATE_ID
            JOIN [CFOP] cf ON csf.CFOP_ID = cf.PKId
            WHERE od.ORDER_ID = @OrderId AND od.QUANTITY > 0";
        var rows = await _connection.QueryAsync<CfopRow>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: ct)).ConfigureAwait(false);
        return rows.Select(r => new CfopItemDto
        {
            Id = (byte)r.Id,
            Code = r.Code ?? "",
            Description = r.Description ?? "",
            DisplayText = $"{r.Code} - {r.Description}"
        }).ToList();
    }

    // --- Emissão em Lote (Story 10.8) ---

    private const string NfScheduleSysControlCode = "NF_SCHEDULE";

    public async Task<IReadOnlyList<ScheduleRowDto>> GetCurrentSchedulesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CONVERT(VARCHAR(10), o.SYS_CREATION_DATE, 103) AS Date,
                   CONVERT(VARCHAR(8), o.SYS_CREATION_DATE, 108) AS Hour,
                   o.PKId AS OrderId,
                   ISNULL(o.RECEIPT, 0) AS Receipt,
                   c.FANTASY_NAME AS FantasyName,
                   o.SALES_AGENT AS SalesAgent,
                   o.STATUS AS Status,
                   o.SITE_ORDER_ID AS SiteOrderId,
                   ISNULL(sch.STATUS, 'NS') AS SchStatus,
                   ISNULL(sch.ERROR_CODE, '') AS SchErrorCode,
                   ISNULL(o.DISCOUNT, 0) AS SchDiscount,
                   ISNULL(sch.DESCRIPTION, '') AS SchDesc,
                   ISNULL(sch.SEND_TEXT, '') AS SendText,
                   sch.XML_FILE_NAME AS XmlFileName,
                   sch.PDF_FILE_NAME AS PdfFileName,
                   o.NFE_KEY AS NfeKey
            FROM [ORDER] o
            LEFT JOIN [NF_SCHEDULE] sch ON sch.ORDER_ID = o.PKId
            INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId
            WHERE o.NFE_CANCEL_PROTOCOL IS NULL
              AND (
                  (o.STATUS NOT IN ('E', 'C'))
                  OR (o.STATUS = 'E' AND ISNULL(sch.STATUS, 'NS') IN ('ERROR', 'SCHED', 'RUN'))
                  OR (o.STATUS = 'E' AND ISNULL(sch.STATUS, 'NS') = 'NF' AND o.SENT_DATE > DATEADD(DAY, -1, GETDATE()))
              )
            ORDER BY o.PKId DESC";
        var rows = await _connection.QueryAsync<ScheduleRowRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(r => new ScheduleRowDto
        {
            OrderId = r.OrderId,
            Date = r.Date ?? "",
            Hour = r.Hour ?? "",
            Receipt = r.Receipt ?? 0,
            FantasyName = r.FantasyName ?? "",
            SalesAgent = r.SalesAgent ?? "",
            Status = r.Status ?? "",
            SiteOrderId = r.SiteOrderId,
            SchStatus = r.SchStatus ?? "NS",
            SchErrorCode = r.SchErrorCode ?? "",
            SchDiscount = r.SchDiscount ?? 0,
            SchDesc = r.SchDesc ?? "",
            SendText = r.SendText ?? "",
            XmlFileName = ResolveScheduleXmlFileName(r),
            PdfFileName = ResolveSchedulePdfFileName(r)
        }).ToList();
    }

    private static string? ResolveSchedulePdfFileName(ScheduleRowRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.PdfFileName))
            return r.PdfFileName!.Trim();
        if ((r.SchStatus == "NF" || r.SchStatus == "EMAIL") && !string.IsNullOrWhiteSpace(r.NfeKey))
            return "DFE" + r.NfeKey!.Trim() + ".pdf";
        return null;
    }

    private static string? ResolveScheduleXmlFileName(ScheduleRowRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.XmlFileName))
            return r.XmlFileName!.Trim();
        if ((r.SchStatus == "NF" || r.SchStatus == "EMAIL") && !string.IsNullOrWhiteSpace(r.NfeKey))
            return "DFE" + r.NfeKey!.Trim() + ".xml";
        return null;
    }

    public async Task<bool> GetNfScheduleFlagAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT VALUE FROM SYS_CONTROL WHERE CODE = @Code";
        var value = await _connection.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, new { Code = NfScheduleSysControlCode }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return string.Equals(value?.Trim(), "1", StringComparison.Ordinal);
    }

    public async Task SetNfScheduleFlagAsync(bool value, CancellationToken cancellationToken = default)
    {
        var v = value ? "1" : "0";
        const string sql = @"
            MERGE SYS_CONTROL AS t
            USING (SELECT @Code AS CODE) AS s ON t.CODE = s.CODE
            WHEN MATCHED THEN UPDATE SET VALUE = @Value
            WHEN NOT MATCHED THEN INSERT (CODE, VALUE) VALUES (@Code, @Value);";
        await _connection.ExecuteAsync(new CommandDefinition(sql, new { Code = NfScheduleSysControlCode, Value = v }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task SaveScheduleBatchAsync(IReadOnlyList<ScheduleItemInput> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return;
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
        using var trx = _connection.BeginTransaction();
        try
        {
            var orderIds = items.Select(i => i.OrderId).Distinct().ToList();
            const string sqlOrderInfo = "SELECT o.PKId AS OrderId, o.SALES_AGENT AS SalesAgent, c.FANTASY_NAME AS FantasyName FROM [ORDER] o INNER JOIN [CLIENT] c ON o.CLIENT_ID = c.PKId WHERE o.PKId IN @OrderIds";
            var orderInfos = (await _connection.QueryAsync<ScheduleOrderInfoRow>(new CommandDefinition(sqlOrderInfo, new { OrderIds = orderIds }, cancellationToken: cancellationToken, transaction: trx)).ConfigureAwait(false)).ToDictionary(r => r.OrderId);

            foreach (var item in items)
            {
                const string sqlGetSch = "SELECT STATUS FROM [NF_SCHEDULE] WHERE ORDER_ID = @OrderId";
                var schStatus = await _connection.ExecuteScalarAsync<string>(new CommandDefinition(sqlGetSch, new { item.OrderId }, cancellationToken: cancellationToken, transaction: trx)).ConfigureAwait(false);
                if (string.Equals(schStatus, "NF", StringComparison.Ordinal))
                    continue;
                const string sqlDel = "DELETE FROM [NF_SCHEDULE] WHERE ORDER_ID = @OrderId";
                await _connection.ExecuteAsync(new CommandDefinition(sqlDel, new { item.OrderId }, cancellationToken: cancellationToken, transaction: trx)).ConfigureAwait(false);
                if (item.IsSelected)
                {
                    const string sqlUpdOrder = "UPDATE [ORDER] SET DISCOUNT = @Discount WHERE PKId = @OrderId";
                    await _connection.ExecuteAsync(new CommandDefinition(sqlUpdOrder, new { item.OrderId, item.Discount }, cancellationToken: cancellationToken, transaction: trx)).ConfigureAwait(false);
                    if (orderInfos.TryGetValue(item.OrderId, out var info))
                    {
                        const string sqlIns = @"INSERT INTO [NF_SCHEDULE] (ORDER_ID, ORIGEM, NOME_CLIENTE, SYS_CREATION_DATE, STATUS, DISCOUNT)
                            VALUES (@OrderId, @Origem, @NomeCliente, GETDATE(), 'SCHED', @Discount)";
                        await _connection.ExecuteAsync(new CommandDefinition(sqlIns, new
                        {
                            item.OrderId,
                            Origem = info.SalesAgent ?? "",
                            NomeCliente = info.FantasyName ?? "",
                            item.Discount
                        }, cancellationToken: cancellationToken, transaction: trx)).ConfigureAwait(false);
                    }
                }
            }
            trx.Commit();
        }
        catch
        {
            trx.Rollback();
            throw;
        }
        await SetNfScheduleFlagAsync(true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> PublishNfScheduleSnsAsync(CancellationToken cancellationToken = default)
    {
        var topicArn = (_configuration["NFe:Schedule:SnsTopicArn"] ?? _configuration["AWS:SnsTopicArn"])?.Trim();
        if (string.IsNullOrWhiteSpace(topicArn))
        {
            _logger.LogWarning("NFe Schedule SNS: tópico não configurado. Configure NFe:Schedule:SnsTopicArn (ou AWS:SnsTopicArn) no appsettings para acionar a Lambda. Agendamento foi salvo, mas a emissão em lote não será disparada.");
            return false;
        }
        try
        {
            var region = _configuration["NFe:Schedule:SnsRegion"] ?? _configuration["AWS:Region"] ?? "us-east-1";
            var regionEnd = Amazon.RegionEndpoint.GetBySystemName(region);
            using var client = CreateSnsClient(regionEnd);
            await client.PublishAsync(new Amazon.SimpleNotificationService.Model.PublishRequest
            {
                TopicArn = topicArn,
                Message = "NF"
            }, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("NFe Schedule SNS: mensagem 'NF' publicada no tópico {TopicArn}. Lambda será acionada.", topicArn);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NFe Schedule SNS: falha ao publicar no tópico {TopicArn}. Verifique credenciais AWS e permissões do tópico. Agendamento foi salvo, mas a Lambda não foi disparada.", topicArn);
            return false;
        }
    }

    /// <summary>
    /// Creates SNS client. If NFe:Schedule:CredentialsFile and CredentialsProfile are set, uses that file (same as legacy C:\lion\cred.txt + basic_profile); otherwise uses default credential chain (e.g. EC2 instance role).
    /// </summary>
    private Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient CreateSnsClient(Amazon.RegionEndpoint regionEnd)
    {
        var credentialsFile = (_configuration["NFe:Schedule:CredentialsFile"] ?? _configuration["AWS:CredentialsFile"])?.Trim();
        var profileName = (_configuration["NFe:Schedule:CredentialsProfile"] ?? _configuration["AWS:CredentialsProfile"])?.Trim();
        if (!string.IsNullOrWhiteSpace(credentialsFile) && !string.IsNullOrWhiteSpace(profileName))
        {
            var chain = new CredentialProfileStoreChain(credentialsFile);
            if (chain.TryGetAWSCredentials(profileName, out var awsCredentials))
            {
                _logger.LogDebug("NFe Schedule SNS: usando credenciais do arquivo {File}, perfil {Profile}.", credentialsFile, profileName);
                return new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient(awsCredentials, regionEnd);
            }
            _logger.LogWarning("NFe Schedule SNS: arquivo {File} ou perfil {Profile} não encontrado; usando cadeia padrão de credenciais.", credentialsFile, profileName);
        }
        return new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient(regionEnd);
    }

    public async Task<NFScheduleResultDto> EmitNfeForScheduleAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sqlOrder = "SELECT STATUS, ISNULL(RECEIPT,0) AS Receipt, ISNULL(NFE_RECEIPT,'') AS NfeReceipt, ISNULL(NFE_PROTOCOL_RESULT,'') AS NfeProtocolResult FROM [ORDER] WHERE PKId = @OrderId";
        var row = await _connection.QuerySingleOrDefaultAsync<OrderScheduleRow>(new CommandDefinition(sqlOrder, new { OrderId = orderId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null)
            return new NFScheduleResultDto { RESULT_CODE = "ERROR", RESULT_MESSAGE = "Pedido " + orderId + " não encontrado" };
        if (row.Status != "F" && row.Status != "E")
            return new NFScheduleResultDto { RESULT_CODE = "ERROR", RESULT_MESSAGE = "Pedido " + orderId + " com status inválido" };
        if (!string.IsNullOrEmpty(row.NfeReceipt) && row.NfeProtocolResult == "100")
            return new NFScheduleResultDto { RESULT_CODE = "ERROR", RESULT_MESSAGE = "Pedido " + orderId + " já tem NF emitida" };

        int reimpId = 0;
        if (!string.IsNullOrEmpty(row.NfeReceipt) && row.NfeProtocolResult != "100" && row.NfeProtocolResult != "204" && row.NfeProtocolResult != "539")
            reimpId = row.Receipt;

        string nfeNumber = reimpId > 0 ? ("R" + reimpId) : (await GetNextReceiptNoAsync(cancellationToken).ConfigureAwait(false)).ToString();
        var request = new EmitNfeRequest
        {
            OrderId = orderId,
            NfeNumber = nfeNumber,
            FreightEmitenteDestinatario = 0,
            CfopId = 0
        };
        var result = await EmitNfeAsync(request, cancellationToken).ConfigureAwait(false);
        var pdfName = string.IsNullOrEmpty(result.PdfRelativePath) ? null : Path.GetFileName(result.PdfRelativePath);
        var xmlName = string.IsNullOrEmpty(result.XmlRelativePath) ? null : Path.GetFileName(result.XmlRelativePath);

        const string sqlUpdateSch = @"
            UPDATE NF_SCHEDULE SET STATUS = @Status, DESCRIPTION = @Description, PDF_FILE_NAME = ISNULL(@PdfFileName, ''), XML_FILE_NAME = ISNULL(@XmlFileName, '')
            WHERE ORDER_ID = @OrderId";
        var status = result.Success ? "NF" : "ERROR";
        var description = (result.Success ? "100 - " : "ERROR - ") + (result.Message ?? (result.Success ? "OK" : "Erro na emissão"));
        if (result.Success && !string.IsNullOrEmpty(pdfName))
            description += " | PDF: " + pdfName;
        if (result.Success && !string.IsNullOrEmpty(xmlName))
            description += " | XML: " + xmlName;
        await _connection.ExecuteAsync(new CommandDefinition(sqlUpdateSch, new
        {
            Status = status,
            Description = description,
            PdfFileName = pdfName ?? "",
            XmlFileName = xmlName ?? "",
            OrderId = orderId
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (result.Success)
            return new NFScheduleResultDto { RESULT_CODE = "100", RESULT_MESSAGE = result.Message ?? "OK", PDF_FILE_NAME = pdfName, XML_FILE_NAME = xmlName };
        return new NFScheduleResultDto { RESULT_CODE = "ERROR", RESULT_MESSAGE = result.Message ?? "Erro na emissão", PDF_FILE_NAME = null, XML_FILE_NAME = null };
    }

    public Task ReleaseNfScheduleAsync(CancellationToken cancellationToken = default) => SetNfScheduleFlagAsync(false, cancellationToken);

    private sealed class ScheduleRowRow
    {
        public string? Date { get; init; }
        public string? Hour { get; init; }
        public int OrderId { get; init; }
        public int? Receipt { get; init; }
        public string? FantasyName { get; init; }
        public string? SalesAgent { get; init; }
        public string? Status { get; init; }
        public string? SiteOrderId { get; init; }
        public string? SchStatus { get; init; }
        public string? SchErrorCode { get; init; }
        public decimal? SchDiscount { get; init; }
        public string? SchDesc { get; init; }
        public string? SendText { get; init; }
        public string? XmlFileName { get; init; }
        public string? PdfFileName { get; init; }
        public string? NfeKey { get; init; }
    }

    private sealed class ScheduleOrderInfoRow
    {
        public int OrderId { get; init; }
        public string? SalesAgent { get; init; }
        public string? FantasyName { get; init; }
    }

    private sealed class OrderScheduleRow
    {
        public string? Status { get; init; }
        public int Receipt { get; init; }
        public string? NfeReceipt { get; init; }
        public string? NfeProtocolResult { get; init; }
    }

    private sealed class OrderInfoRow
    {
        public string? ClientName { get; init; }
        public string? Status { get; init; }
        public int? Receipt { get; init; }
        public string? NfeReceipt { get; init; }
        public string? NfeProtocolResult { get; init; }
        public string? NfeProtocol { get; init; }
        public string? NfeKey { get; init; }
        public int? ProductCount { get; init; }
        public string? Address { get; init; }
        public decimal? ShipmentCost { get; init; }
        public decimal? Discount { get; init; }
        public decimal? Credit { get; init; }
        public decimal? OtherExpenses { get; init; }
    }

    private sealed class CfopRow
    {
        public int Id { get; init; }
        public string? Code { get; init; }
        public string? Description { get; init; }
    }

    private sealed class TransportRow
    {
        public int Id { get; init; }
        public string? SocialName { get; init; }
    }

    private sealed class LastOrderRow
    {
        public int OrderId { get; init; }
        public string? ClientFantasyName { get; init; }
        public string? CityName { get; init; }
        public string? StateCode { get; init; }
        public int? Receipt { get; init; }
        public string? NfeReceipt { get; init; }
        public string? NfeProtocol { get; init; }
        public string? NfeCancelProtocol { get; init; }
    }

    private sealed class LastEmittedNfeRow
    {
        public int OrderId { get; init; }
        public int? Receipt { get; init; }
        public string? NfeReceipt { get; init; }
        public string? NfeProtocol { get; init; }
        public string? NfeCancelProtocol { get; init; }
        public string? NfeKey { get; init; }
        public string? SocialName { get; init; }
    }

    private sealed class OrderNfeDetailForDanfeRow
    {
        public int OrderId { get; init; }
        public string? ClientName { get; init; }
        public string? NfeKey { get; init; }
        public string? NfeProtocol { get; init; }
        public string? NfeCancelProtocol { get; init; }
        public string? NfeReceipt { get; init; }
        public int? Receipt { get; init; }
    }

    private sealed class PendingOutboundNfeRow
    {
        public int OrderId { get; init; }
        public int? Receipt { get; init; }
        public string? NfeReceipt { get; init; }
        public string? NfeProtocol { get; init; }
        public string? NfeCancelProtocol { get; init; }
        public string? NfeKey { get; init; }
        public string? NfesNo { get; init; }
        public string? SocialName { get; init; }
    }

    private sealed class PendingInboundNfeRow
    {
        public int ReceiptNo { get; init; }
        public int? InternalReceipt { get; init; }
        public string? NfeReceipt { get; init; }
        public string? NfeProtocol { get; init; }
        public string? NfeCancelProtocol { get; init; }
        public string? NfeKey { get; init; }
        public string? SocialName { get; init; }
    }

    private sealed class ReceiptInNfeDetailRow
    {
        public int ReceiptNo { get; init; }
        public string? SupplierName { get; init; }
        public string? NfeKey { get; init; }
        public string? NfeProtocol { get; init; }
        public string? NfeCancelProtocol { get; init; }
        public string? NfeReceipt { get; init; }
        public int? InternalReceipt { get; init; }
    }

    private sealed class NfesPrintInfoRow
    {
        public int OrderId { get; init; }
        public string? NfesNo { get; init; }
        public string? NfesCheckCode { get; init; }
        public string? NfesChaveAcesso { get; init; }
        public string? ClientEmail { get; init; }
        public int NfesEmailCount { get; init; }
    }

    private sealed class NfeCancelInfoRow
    {
        public int OrderOrReceiptInId { get; init; }
        public string? NfeKey { get; init; }
        public string? NfeProtocol { get; init; }
    }

    private sealed class CanceledReceiptRow
    {
        public int ReceiptNo { get; init; }
        public DateTime CancelDate { get; init; }
        public string? Memo { get; init; }
    }

    private sealed class LastCcEventRow
    {
        public int ReceiptNo { get; init; }
        public int OrderId { get; init; }
        public string? NfeKey { get; init; }
        public string? Protocol { get; init; }
        public string? Reason { get; init; }
        public DateTime SysCreationDate { get; init; }
        public string? UserId { get; init; }
        public string? Email { get; init; }
    }

    private sealed class DestRow
    {
        public string? PersonType { get; init; }
        public string? CpfCnpj { get; init; }
        public string? Nome { get; init; }
        public string? Logradouro { get; init; }
        public string? Numero { get; init; }
        public string? Complemento { get; init; }
        public string? Bairro { get; init; }
        public string? CodigoMun { get; init; }
        public string? Municipio { get; init; }
        public string? Uf { get; init; }
        public string? Cep { get; init; }
        public string? Fone { get; init; }
        public string? Email { get; init; }
        public string? Ie { get; init; }
    }

    private sealed class OrderDetailRow
    {
        public decimal Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal { get; init; }
        public string? ProductCode { get; init; }
        public string? ProductName { get; init; }
        public string? Ncm { get; init; }
        public string? CstbId { get; init; }
    }
}
