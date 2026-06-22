using System.Data;
using Dapper;
using EUROERP.Application.Nfes;
using EUROERP.Infrastructure.Nfes.PrefeituraSp;
using Microsoft.Extensions.Logging;

namespace EUROERP.Infrastructure.Nfes;

public sealed class NfesCancellationService : INfesCancellationService
{
    private readonly IDbConnection _connection;
    private readonly INfesConfigProvider _configProvider;
    private readonly INfesCertificateProvider _certificateProvider;
    private readonly INfesSimplissClient _simplissClient;
    private readonly INfesPrefeituraClient _prefeituraClient;
    private readonly ILogger<NfesCancellationService> _logger;

    public NfesCancellationService(
        IDbConnection connection,
        INfesConfigProvider configProvider,
        INfesCertificateProvider certificateProvider,
        INfesSimplissClient simplissClient,
        INfesPrefeituraClient prefeituraClient,
        ILogger<NfesCancellationService> logger)
    {
        _connection = connection;
        _configProvider = configProvider;
        _certificateProvider = certificateProvider;
        _simplissClient = simplissClient;
        _prefeituraClient = prefeituraClient;
        _logger = logger;
    }

    public async Task<CancelNfesResult> CancelAsync(CancelNfesRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NfesNo < 1)
            return Fail("Informe o número da NFS-e de serviços.");

        var memo = NormalizeMemo(request.Memo);
        if (memo == null)
            return Fail("O motivo deve ter entre 15 e 200 caracteres.");

        var order = await LoadOrderByNfesNoAsync(request.NfesNo, cancellationToken).ConfigureAwait(false);
        if (order == null)
            return Fail($"NFS-e de serviços #{request.NfesNo} não encontrada em nenhum pedido.");

        if (await IsReceiptCanceledAsync(request.NfesNo, cancellationToken).ConfigureAwait(false))
            return Fail($"Não é possível cancelar. A nota {request.NfesNo} já foi cancelada.");

        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var userId = FitUserId(request.UserId);
        var applicationId = FitApplicationId(request.ApplicationId);

        try
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            using var tx = _connection.BeginTransaction();
            try
            {
                await InsertReceiptCancelAsync(request.NfesNo, request.CancelDate, memo, userId, applicationId, tx, cancellationToken)
                    .ConfigureAwait(false);

                if (config.UseSimpliss)
                {
                    var cancelOutcome = await CancelSimplissForOrderAsync(config, order, memo, request.MotivoCode, cancellationToken)
                        .ConfigureAwait(false);
                    if (!cancelOutcome.Success)
                    {
                        tx.Rollback();
                        return Fail(cancelOutcome.ErrorMessage ?? "Falha ao cancelar NFS-e no Simpliss.");
                    }

                    await TrySaveCancelEventXmlAsync(order.OrderId, config, cancelOutcome.EventoXml, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    var cancelOutcome = await CancelPrefeituraSpAsync(config, order.NfesNo ?? "", order.NfesCheckCode ?? "", cancellationToken)
                        .ConfigureAwait(false);
                    if (!cancelOutcome.Success)
                    {
                        tx.Rollback();
                        return Fail(cancelOutcome.Message);
                    }
                }

                await ClearOrderNfesFieldsAsync(order.OrderId, request.NfesNo, tx, cancellationToken).ConfigureAwait(false);
                tx.Commit();

                return new CancelNfesResult
                {
                    Success = true,
                    Message = "Cancelamento efetuado com sucesso.",
                    OrderId = order.OrderId
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NFES cancel failed for NFS-e {NfesNo}", request.NfesNo);
            return Fail(ex.Message);
        }
    }

    public async Task<CancelNfesResult> CancelManualAsync(CancelNfesManualRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NfesNo < 1)
            return Fail("Informe o número da NFS-e de serviços.");

        var memo = NormalizeMemo(request.Memo);
        if (memo == null)
            return Fail("O motivo deve ter entre 15 e 200 caracteres.");

        if (request.RegisterLocalCancel
            && await IsReceiptCanceledAsync(request.NfesNo, cancellationToken).ConfigureAwait(false))
            return Fail($"Não é possível cancelar. A nota {request.NfesNo} já foi cancelada localmente.");

        var config = await _configProvider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var userId = FitUserId(request.UserId);
        var applicationId = FitApplicationId(request.ApplicationId);

        string? chaveSimpliss = null;
        if (config.UseSimpliss)
        {
            chaveSimpliss = NormalizeChaveAcesso(request.ChaveAcesso);
            if (chaveSimpliss == null)
                return Fail("Informe a chave de acesso NFS-e (50 dígitos).");
        }
        else if (string.IsNullOrWhiteSpace(request.CodigoVerificacao?.Trim()))
        {
            return Fail("Informe o código de verificação da NFS-e.");
        }

        int? orderId = request.OrderId;
        if (orderId is > 0 && !await OrderExistsAsync(orderId.Value, cancellationToken).ConfigureAwait(false))
            return Fail($"Pedido (OS) #{orderId} não encontrado.");

        try
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            using var tx = _connection.BeginTransaction();
            try
            {
                string? eventoXml = null;

                if (config.UseSimpliss)
                {
                    var cancelOutcome = await CancelSimplissWithChaveAsync(config, chaveSimpliss!, memo, request.MotivoCode, cancellationToken)
                        .ConfigureAwait(false);
                    if (!cancelOutcome.Success)
                    {
                        tx.Rollback();
                        return Fail(cancelOutcome.ErrorMessage ?? "Falha ao cancelar NFS-e no Simpliss.");
                    }

                    eventoXml = cancelOutcome.EventoXml;
                }
                else
                {
                    var cancelOutcome = await CancelPrefeituraSpAsync(
                            config,
                            request.NfesNo.ToString(),
                            request.CodigoVerificacao.Trim(),
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (!cancelOutcome.Success)
                    {
                        tx.Rollback();
                        return Fail(cancelOutcome.Message);
                    }
                }

                if (request.RegisterLocalCancel)
                {
                    await InsertReceiptCancelAsync(request.NfesNo, request.CancelDate, memo, userId, applicationId, tx, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (orderId is > 0)
                {
                    await ClearOrderNfesFieldsAsync(orderId.Value, request.NfesNo, tx, cancellationToken).ConfigureAwait(false);
                    await TrySaveCancelEventXmlAsync(orderId.Value, config, eventoXml, cancellationToken).ConfigureAwait(false);
                }

                tx.Commit();

                _logger.LogWarning(
                    "Manual NFES cancel succeeded for NFS-e {NfesNo} by user {UserId}. OrderId={OrderId}, RegisterLocal={RegisterLocal}",
                    request.NfesNo,
                    userId,
                    orderId,
                    request.RegisterLocalCancel);

                return new CancelNfesResult
                {
                    Success = true,
                    Message = "Cancelamento manual efetuado com sucesso.",
                    OrderId = orderId
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual NFES cancel failed for NFS-e {NfesNo}", request.NfesNo);
            return Fail(ex.Message);
        }
    }

    public async Task<IReadOnlyList<NfesCanceledReceiptDto>> GetTodayCanceledAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CANCEL_DATE AS CancelDate, RECEIPT_NO AS ReceiptNo, ISNULL(MEMO, '') AS Memo
            FROM RECEIPT_CANCEL
            WHERE CAST(SYS_CREATION_DATE AS date) = CAST(GETDATE() AS date)
            ORDER BY SYS_CREATION_DATE DESC";

        var rows = await _connection.QueryAsync<NfesCanceledReceiptDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    private async Task<SimplissCancelResponse> CancelSimplissForOrderAsync(
        NfesConfigSnapshot config,
        NfesOrderNfesRow order,
        string memo,
        string motivoCode,
        CancellationToken cancellationToken)
    {
        var chave = ResolveChaveAcesso(order.NfesChaveAcesso, order.NfesCheckCode);
        if (string.IsNullOrWhiteSpace(chave))
            chave = await TryLoadChaveFromLocalXmlAsync(order.OrderId, config, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(chave))
            return new SimplissCancelResponse
            {
                Success = false,
                ErrorMessage = "Pedido sem chave de acesso NFS-e (NFES_CHAVE_ACESSO). Não é possível cancelar via Simpliss."
            };

        return await CancelSimplissWithChaveAsync(config, chave, memo, motivoCode, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SimplissCancelResponse> CancelSimplissWithChaveAsync(
        NfesConfigSnapshot config,
        string chave,
        string memo,
        string motivoCode,
        CancellationToken cancellationToken)
    {
        var pedRegXml = NfesCancelEventBuilder.Build(config, chave, memo, motivoCode);
        var cert = _certificateProvider.GetCertificate();
        var signedDoc = NfesDpsXmlSupport.SignPedRegEvento(pedRegXml, cert);
        var signedXml = signedDoc.OuterXml;

        return await _simplissClient.CancelNfseAsync(chave, signedXml, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(bool Success, string Message)> CancelPrefeituraSpAsync(
        NfesConfigSnapshot config,
        string nfesNo,
        string nfesCheckCode,
        CancellationToken cancellationToken)
    {
        var cert = _certificateProvider.GetCertificate();
        var pedido = NfesPrefeituraCancelBuilder.Build(config, nfesNo, nfesCheckCode, cert);
        var xmlDoc = NfesXmlSupport.SerializePedidoCancelamento(pedido);
        xmlDoc = NfesXmlSupport.SignRpsDocument(xmlDoc, cert);
        var signedXml = xmlDoc.OuterXml.Replace("utf-16", "utf-8", StringComparison.Ordinal);

        var responseXml = await _prefeituraClient.SendCancelamentoAsync(signedXml, cancellationToken).ConfigureAwait(false);
        var retorno = NfesXmlSupport.Deserialize<RetornoCancelamentoNFe>(responseXml);

        if (retorno.Cabecalho?.Sucesso == true)
            return (true, "Cancelamento aceito pela Prefeitura SP.");

        return (false, FormatPrefeituraCancelErrors(retorno));
    }

    private async Task<NfesOrderNfesRow?> LoadOrderByNfesNoAsync(int nfesNo, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP 1
                o.PKId AS OrderId,
                ISNULL(o.NFES_NO, '') AS NfesNo,
                ISNULL(o.RPS_NO, '') AS RpsNo,
                ISNULL(o.NFES_CHECK_CODE, '') AS NfesCheckCode,
                ISNULL(o.NFES_CHAVE_ACESSO, '') AS NfesChaveAcesso
            FROM [ORDER] o
            WHERE o.NFES_NO = @NfesNo
            ORDER BY o.SYS_CREATION_DATE DESC";

        return await _connection.QuerySingleOrDefaultAsync<NfesOrderNfesRow>(
            new CommandDefinition(sql, new { NfesNo = nfesNo.ToString() }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task<bool> IsReceiptCanceledAsync(int receiptNo, CancellationToken cancellationToken)
    {
        const string sql = "SELECT 1 FROM RECEIPT_CANCEL WHERE RECEIPT_NO = @ReceiptNo";
        var exists = await _connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(sql, new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return exists == 1;
    }

    private async Task<bool> OrderExistsAsync(int orderId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT 1 FROM [ORDER] WHERE PKId = @OrderId";
        var exists = await _connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return exists == 1;
    }

    private static async Task InsertReceiptCancelAsync(
        int nfesNo,
        DateTime cancelDate,
        string memo,
        string userId,
        string applicationId,
        IDbTransaction tx,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO RECEIPT_CANCEL
                (SYS_CREATION_DATE, USER_ID, APPLICATION_ID, CANCEL_DATE, RECEIPT_NO, MEMO, RECEIPT_FORM)
            VALUES
                (GETDATE(), @UserId, @ApplicationId, @CancelDate, @ReceiptNo, @Memo, @ReceiptForm)";

        await tx.Connection!.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                UserId = userId,
                ApplicationId = applicationId,
                CancelDate = cancelDate.Date,
                ReceiptNo = nfesNo,
                Memo = memo,
                ReceiptForm = nfesNo
            }, tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task ClearOrderNfesFieldsAsync(int orderId, int receiptNo, IDbTransaction tx, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE [ORDER]
            SET RPS_NO = '', NFES_NO = '', NFES_CHECK_CODE = '', NFES_CHAVE_ACESSO = NULL
            WHERE PKId = @OrderId";

        await tx.Connection!.ExecuteAsync(
            new CommandDefinition(sql, new { OrderId = orderId }, tx, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        const string delReceipt = @"
            DELETE FROM RECEIPT
            WHERE RECEIPT_NO = @ReceiptNo AND ORDER_ID = @OrderId AND TYPE = 'S'";

        await tx.Connection!.ExecuteAsync(
            new CommandDefinition(delReceipt, new { ReceiptNo = receiptNo, OrderId = orderId }, tx, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    private static string? NormalizeMemo(string? memo)
    {
        var text = memo?.Trim() ?? "";
        return text.Length is >= 15 and <= 200 ? text : null;
    }

    private static string? NormalizeChaveAcesso(string? chave)
    {
        var digits = NfesTextHelper.CleanDigits(chave?.Trim() ?? "");
        if (digits.Length < 50)
            return null;
        return digits.Length == 50 ? digits : digits[^50..];
    }

    private static string? ResolveChaveAcesso(string? nfesChaveAcesso, string? checkCode)
    {
        foreach (var candidate in new[] { nfesChaveAcesso, checkCode })
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;
            var digits = NfesTextHelper.CleanDigits(candidate.Trim());
            if (digits.Length >= 50)
                return digits.Length == 50 ? digits : digits[^50..];
        }

        return null;
    }

    private static async Task<string?> TryLoadChaveFromLocalXmlAsync(int orderId, NfesConfigSnapshot config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.XmlPath))
            return null;

        var nfsePath = Path.Combine(config.XmlPath, "S" + orderId, orderId + "-nfse.xml");
        if (!File.Exists(nfsePath))
            return null;

        try
        {
            var xml = await File.ReadAllTextAsync(nfsePath, cancellationToken).ConfigureAwait(false);
            var idx = xml.IndexOf("chNFSe", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null;
            var start = xml.IndexOf('>', idx) + 1;
            var end = xml.IndexOf('<', start);
            if (start <= 0 || end <= start)
                return null;
            var chave = NfesTextHelper.CleanDigits(xml[start..end]);
            return chave.Length >= 50 ? chave : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task TrySaveCancelEventXmlAsync(int orderId, NfesConfigSnapshot config, string? eventoXml, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.XmlPath) || string.IsNullOrWhiteSpace(eventoXml))
            return;

        try
        {
            var folder = Path.Combine(config.XmlPath, "S" + orderId);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, orderId + "-nfse-cancel.xml");
            await File.WriteAllTextAsync(path, eventoXml, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal.
        }
    }

    private static string FormatPrefeituraCancelErrors(RetornoCancelamentoNFe retorno)
    {
        var parts = new List<string>();
        if (retorno.Erro is { Length: > 0 })
        {
            foreach (var e in retorno.Erro)
                parts.Add($"Erro {e.Codigo}: {e.Descricao}");
        }

        if (retorno.Alerta is { Length: > 0 })
        {
            foreach (var a in retorno.Alerta)
                parts.Add($"Alerta {a.Codigo}: {a.Descricao}");
        }

        return parts.Count > 0
            ? string.Join(Environment.NewLine, parts)
            : "Prefeitura SP rejeitou o cancelamento.";
    }

    private static string FitUserId(string? userId) =>
        NfesTextHelper.FitDb(string.IsNullOrWhiteSpace(userId) ? "SYS" : userId.Trim(), 20);

    private static string FitApplicationId(string? applicationId) =>
        NfesTextHelper.FitDb(string.IsNullOrWhiteSpace(applicationId) ? "EUROERP" : applicationId.Trim(), 8);

    private static CancelNfesResult Fail(string message) =>
        new() { Success = false, Message = message };

    private sealed class NfesOrderNfesRow
    {
        public int OrderId { get; init; }
        public string? NfesNo { get; init; }
        public string? RpsNo { get; init; }
        public string? NfesCheckCode { get; init; }
        public string? NfesChaveAcesso { get; init; }
    }
}
