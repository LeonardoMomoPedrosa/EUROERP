using System.Data;
using Dapper;
using EUROERP.Application.NFe;

namespace EUROERP.Infrastructure.NFe;

public class ReceiptInNfeDataService : IReceiptInNfeDataService
{
    private readonly IDbConnection _connection;

    public ReceiptInNfeDataService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<ReceiptInNfeDataDto?> GetByReceiptNoAsync(int receiptNo, byte version, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT rid.RECEIPT_NO AS ReceiptNo,
                   rid.VERSION AS Version,
                   rid.SUPPLIER_ID AS SupplierId,
                   su.SOCIAL_NAME AS SupplierName,
                   rid.CFOP_ID AS CfopId,
                   rid.INOUT AS InOut,
                   rid.SHIPM_ORIGIN AS ShipmOrigin,
                   ISNULL(rid.SHIPMENT, 0) AS Shipment,
                   rid.NFE_REF AS NfeRef,
                   rid.OBS AS Obs,
                   ISNULL(rid.ST_AMOUNT, 0) AS StAmount,
                   ISNULL(rid.ST_BASE_CALC, 0) AS StBaseCalc,
                   ISNULL(rid.BASE_CALC, 0) AS BaseCalc,
                   ISNULL(rid.ICMS_PERC, 0) AS IcmsPerc,
                   ISNULL(rid.CONVERSION, 1) AS Conversion,
                   ISNULL(rid.VOLUMES, 0) AS Volumes,
                   ISNULL(rid.WEIGHT_GROSS, 0) AS WeightGross,
                   ISNULL(rid.WEIGHT_NET, 0) AS WeightNet,
                   rid.ESPECIE AS Especie,
                   ISNULL(rid.OTHER_AMOUNT, 0) AS OtherAmount,
                   rid.CSOSN AS Csosn,
                   rid.NFE_KEY AS NfeKey,
                   rid.NFE_PROTOCOL AS NfeProtocol
            FROM [RECEIPT_IN_DATA] rid
            JOIN [SUPPLIER] su ON su.PKId = rid.SUPPLIER_ID
            WHERE rid.RECEIPT_NO = @ReceiptNo AND rid.VERSION = @Version";
        var row = await _connection.QuerySingleOrDefaultAsync<ReceiptInNfeDataDto>(
            new CommandDefinition(sql, new { ReceiptNo = receiptNo, Version = version }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (row == null) return null;
        SplitObs(row);
        return row;
    }

    public async Task<int?> ResolveReceiptNoByInternalReceiptAsync(int internalReceipt, CancellationToken cancellationToken = default)
    {
        return await _connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SELECT RECEIPT_NO FROM [RECEIPT_IN_DATA] WHERE INTERNAL_RECEIPT = @InternalReceipt",
                new { InternalReceipt = internalReceipt },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<SaveReceiptInNfeResult> SaveV1Async(SaveReceiptInNfeV1Request request, CancellationToken cancellationToken = default)
    {
        if (request.ReceiptNo <= 0)
            return Fail("Informe o número da NF.");
        if (request.SupplierId <= 0)
            return Fail("Selecione o fornecedor.");

        var exists = await ExistsAsync(request.ReceiptNo, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            const string update = @"
                UPDATE [RECEIPT_IN_DATA] SET
                    SUPPLIER_ID = @SupplierId,
                    CFOP_ID = @CfopId,
                    INOUT = @InOut,
                    SHIPM_ORIGIN = @ShipmOrigin,
                    SHIPMENT = @Shipment,
                    ST_AMOUNT = @StAmount,
                    NFE_REF = @NfeRef,
                    OBS = @Obs,
                    VERSION = 1,
                    SYS_UPDATE_DATE = GETDATE(),
                    USER_ID = @UserId,
                    APPLICATION_ID = @ApplicationId
                WHERE RECEIPT_NO = @ReceiptNo";
            await _connection.ExecuteAsync(new CommandDefinition(update, MapV1(request), cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            const string insert = @"
                INSERT INTO [RECEIPT_IN_DATA] (
                    CONVERSION, II, BASE_CALC, ICMS, PRODUCT_COST, SHIPMENT, IPI, PIS, COFINS, OTHER_AMOUNT,
                    ICMS_PERC, ST_AMOUNT, ST_BASE_CALC, WEIGHT_GROSS, WEIGHT_NET, VERSION, VOLUMES, ESPECIE,
                    RECEIPT_NO, CFOP_ID, CSOSN, SHIPM_ORIGIN, INOUT, TYPE, NFE_REF, OBS,
                    SYS_CREATION_DATE, APPLICATION_ID, USER_ID, SUPPLIER_ID)
                VALUES (
                    1, 0, 0, 0, 0, @Shipment, 0, 0, 0, 0,
                    0, @StAmount, 0, 0, 0, 1, 0, '',
                    @ReceiptNo, @CfopId, '101', @ShipmOrigin, @InOut, 'P', @NfeRef, @Obs,
                    GETDATE(), @ApplicationId, @UserId, @SupplierId)";
            await _connection.ExecuteAsync(new CommandDefinition(insert, MapV1(request), cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        return Ok(request.ReceiptNo);
    }

    public async Task<SaveReceiptInNfeResult> SaveV2Async(SaveReceiptInNfeV2Request request, CancellationToken cancellationToken = default)
    {
        if (request.ReceiptNo <= 0)
            return Fail("Informe o número da NF.");
        if (request.SupplierId <= 0)
            return Fail("Selecione o fornecedor.");

        var obs = JoinObs(request.Obs1, request.Obs2, request.Obs3);
        var exists = await ExistsAsync(request.ReceiptNo, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            const string update = @"
                UPDATE [RECEIPT_IN_DATA] SET
                    CONVERSION = @Conversion,
                    II = 0,
                    PRODUCT_COST = 0,
                    BASE_CALC = @BaseCalc,
                    SHIPMENT = @Shipment,
                    IPI = 0,
                    PIS = 0,
                    COFINS = 0,
                    OTHER_AMOUNT = @OtherAmount,
                    ICMS_PERC = @IcmsPerc,
                    ST_AMOUNT = @StAmount,
                    ST_BASE_CALC = @StBaseCalc,
                    WEIGHT_GROSS = @WeightGross,
                    WEIGHT_NET = @WeightNet,
                    VOLUMES = @Volumes,
                    ESPECIE = @Especie,
                    CFOP_ID = @CfopId,
                    CSOSN = @Csosn,
                    TYPE = 'P',
                    NFE_REF = @NfeRef,
                    VERSION = 2,
                    SUPPLIER_ID = @SupplierId,
                    SHIPM_ORIGIN = @ShipmOrigin,
                    SYS_UPDATE_DATE = GETDATE(),
                    INOUT = @InOut,
                    OBS = @Obs,
                    USER_ID = @UserId,
                    APPLICATION_ID = @ApplicationId
                WHERE RECEIPT_NO = @ReceiptNo";
            await _connection.ExecuteAsync(new CommandDefinition(update, MapV2(request, obs), cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            const string insert = @"
                INSERT INTO [RECEIPT_IN_DATA] (
                    CONVERSION, II, BASE_CALC, ICMS, PRODUCT_COST, SHIPMENT, IPI, PIS, COFINS, OTHER_AMOUNT,
                    ICMS_PERC, ST_AMOUNT, ST_BASE_CALC, WEIGHT_GROSS, WEIGHT_NET, VERSION, VOLUMES, ESPECIE,
                    RECEIPT_NO, CFOP_ID, CSOSN, SHIPM_ORIGIN, INOUT, TYPE, NFE_REF, OBS,
                    SYS_CREATION_DATE, APPLICATION_ID, USER_ID, SUPPLIER_ID)
                VALUES (
                    @Conversion, 0, @BaseCalc, 0, 0, @Shipment, 0, 0, 0, @OtherAmount,
                    @IcmsPerc, @StAmount, @StBaseCalc, @WeightGross, @WeightNet, 2, @Volumes, @Especie,
                    @ReceiptNo, @CfopId, @Csosn, @ShipmOrigin, @InOut, 'P', @NfeRef, @Obs,
                    GETDATE(), @ApplicationId, @UserId, @SupplierId)";
            await _connection.ExecuteAsync(new CommandDefinition(insert, MapV2(request, obs), cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        return Ok(request.ReceiptNo);
    }

    public async Task<IReadOnlyList<ReceiptInDetailDto>> GetDetailsAsync(int receiptNo, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT PRODUCT_CODE AS ProductCode,
                   PRODUCT_NAME AS ProductName,
                   QUANTITY AS Quantity,
                   UNIT_PRICE AS UnitPrice,
                   FISCAL_CLASS AS FiscalClass,
                   IPI AS Ipi,
                   ICMS AS Icms
            FROM [RECEIPT_IN_DETAILS]
            WHERE RECEIPT_NO = @ReceiptNo
            ORDER BY PRODUCT_CODE";
        var list = await _connection.QueryAsync<ReceiptInDetailDto>(
            new CommandDefinition(sql, new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task<(bool Success, string Message)> AddDetailAsync(AddReceiptInDetailRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ReceiptNo <= 0)
            return (false, "NF inválida.");
        if (string.IsNullOrWhiteSpace(request.ProductCode))
            return (false, "Informe o código do produto.");
        if (string.IsNullOrWhiteSpace(request.ProductName))
            return (false, "Informe o nome do produto.");
        if (request.Quantity <= 0)
            return (false, "Quantidade inválida.");

        const string delete = "DELETE FROM [RECEIPT_IN_DETAILS] WHERE RECEIPT_NO = @ReceiptNo AND PRODUCT_CODE = @ProductCode";
        await _connection.ExecuteAsync(new CommandDefinition(delete, new { request.ReceiptNo, request.ProductCode }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        const string insert = @"
            INSERT INTO [RECEIPT_IN_DETAILS] (RECEIPT_NO, PRODUCT_CODE, PRODUCT_NAME, QUANTITY, FISCAL_CLASS, IPI, ICMS, UNIT_PRICE)
            VALUES (@ReceiptNo, @ProductCode, @ProductName, @Quantity, @FiscalClass, @Ipi, @Icms, @UnitPrice)";
        await _connection.ExecuteAsync(new CommandDefinition(insert, request, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return (true, "Item incluído.");
    }

    public Task DeleteDetailAsync(int receiptNo, string productCode, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM [RECEIPT_IN_DETAILS] WHERE RECEIPT_NO = @ReceiptNo AND PRODUCT_CODE = @ProductCode";
        return _connection.ExecuteAsync(new CommandDefinition(sql, new { ReceiptNo = receiptNo, ProductCode = productCode }, cancellationToken: cancellationToken));
    }

    private async Task<bool> ExistsAsync(int receiptNo, CancellationToken cancellationToken)
    {
        var n = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(1) FROM [RECEIPT_IN_DATA] WHERE RECEIPT_NO = @ReceiptNo", new { ReceiptNo = receiptNo }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    private static object MapV1(SaveReceiptInNfeV1Request r) => new
    {
        r.ReceiptNo,
        r.SupplierId,
        r.CfopId,
        InOut = NormalizeInOut(r.InOut),
        r.ShipmOrigin,
        r.Shipment,
        StAmount = r.StAmount,
        NfeRef = r.NfeRef ?? "",
        Obs = r.Obs ?? "",
        r.UserId,
        r.ApplicationId
    };

    private static object MapV2(SaveReceiptInNfeV2Request r, string obs) => new
    {
        r.ReceiptNo,
        r.SupplierId,
        r.CfopId,
        InOut = NormalizeInOut(r.InOut),
        r.ShipmOrigin,
        r.Shipment,
        r.BaseCalc,
        r.IcmsPerc,
        StAmount = r.StAmount,
        StBaseCalc = r.StBaseCalc,
        r.OtherAmount,
        r.Conversion,
        r.Volumes,
        r.WeightGross,
        r.WeightNet,
        Especie = r.Especie ?? "",
        Csosn = r.Csosn ?? "102",
        NfeRef = r.NfeRef ?? "",
        Obs = obs,
        r.UserId,
        r.ApplicationId
    };

    private static string NormalizeInOut(string? inOut) => inOut?.Trim().ToUpperInvariant() == "O" ? "O" : "I";

    private static string JoinObs(string? o1, string? o2, string? o3)
    {
        var parts = new[] { o1, o2, o3 }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()).ToArray();
        return string.Join("[LF]", parts);
    }

    private static void SplitObs(ReceiptInNfeDataDto row)
    {
        if (string.IsNullOrEmpty(row.Obs)) return;
        var parts = row.Obs.Split("[LF]", StringSplitOptions.None);
        row.Obs = parts.Length > 0 ? parts[0] : "";
        row.Obs2 = parts.Length > 1 ? parts[1] : "";
        row.Obs3 = parts.Length > 2 ? parts[2] : "";
    }

    private static SaveReceiptInNfeResult Ok(int receiptNo) => new() { Success = true, ReceiptNo = receiptNo, Message = "Dados salvos." };
    private static SaveReceiptInNfeResult Fail(string message) => new() { Success = false, Message = message };
}
