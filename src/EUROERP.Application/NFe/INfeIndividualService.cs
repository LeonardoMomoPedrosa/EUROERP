namespace EUROERP.Application.NFe;

public interface INfeIndividualService
{
    Task<int> GetNextReceiptNoAsync(CancellationToken cancellationToken = default);

    /// <summary>Order info for NFe page (sale only). Returns null if order not found or invalid for NFe.</summary>
    Task<OrderInfoForNfeDto?> GetOrderInfoForNfeAsync(int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CfopItemDto>> GetCfopListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransportSupplierDto>> GetTransportSuppliersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LastOrderForNfeDto>> GetLastPendingClosedOrdersForNfeAsync(int top = 30, CancellationToken cancellationToken = default);

    /// <summary>Last emitted NFe (sales only, NFE_STATUS = 1), for DANFEs page list.</summary>
    Task<IReadOnlyList<LastEmittedNfeDto>> GetLastEmittedNfeListAsync(int top = 50, CancellationToken cancellationToken = default);

    /// <summary>Order NFe detail for DANFEs page (search by order). Returns null if order not found or has no NFE_PROTOCOL.</summary>
    Task<OrderNfeDetailForDanfeDto?> GetOrderNfeDetailForDanfeAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Últimas Saídas (Story 12.3): NFE_STATUS=1 or NFES_NO set. Optional filter by orderId.</summary>
    Task<IReadOnlyList<PendingOutboundNfeDto>> GetPendingOutboundNfeListAsync(int? orderId = null, int top = 15, CancellationToken cancellationToken = default);

    /// <summary>Últimas Entradas (Story 12.3): RECEIPT_IN_DATA. Optional filter by receiptNo (legacy used order textbox).</summary>
    Task<IReadOnlyList<PendingInboundNfeDto>> GetPendingInboundNfeListAsync(int? receiptNo = null, int top = 15, CancellationToken cancellationToken = default);

    /// <summary>Receipt-in NFe detail for Imprimir (authorized or canceled). Null if missing / no key+protocol.</summary>
    Task<ReceiptInNfeDetailForDanfeDto?> GetReceiptInNfeDetailForDanfeAsync(int receiptNo, CancellationToken cancellationToken = default);

    /// <summary>Resolve NFES by number for print panel. Null if not found.</summary>
    Task<NfesPrintInfoDto?> GetNfesPrintInfoByNfesNoAsync(string nfesNo, CancellationToken cancellationToken = default);

    Task<EmitNfeResult> EmitNfeAsync(EmitNfeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Regenera apenas o PDF do DANFE para um pedido que já possui NFe emitida. Retorna os caminhos do PDF/XML para download.</summary>
    Task<EmitNfeResult> RegeneratePdfOnlyAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Resolve NFe by receipt number (sale first, then receipt-in). Returns null if not found or no authorized NFe.</summary>
    Task<NfeCancelInfoDto?> GetNfeCancelInfoByReceiptNoAsync(int receiptNo, CancellationToken cancellationToken = default);

    /// <summary>Cancel NFe at SEFAZ and update local data. Transaction: RECEIPT_CANCEL + SEFAZ; rollback on SEFAZ failure.</summary>
    Task<CancelNfeResult> CancelNfeAsync(CancelNfeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Today's canceled receipts (by SYS_CREATION_DATE).</summary>
    Task<IReadOnlyList<CanceledReceiptDto>> GetTodayCanceledReceiptsAsync(CancellationToken cancellationToken = default);

    /// <summary>Last CCe sequence number for a receipt (sales only). Returns 0 if none.</summary>
    Task<byte> GetLastCcSeqNoAsync(int receiptNo, CancellationToken cancellationToken = default);

    /// <summary>Last Carta de Correção events for a receipt (sales only).</summary>
    Task<IReadOnlyList<LastCcEventDto>> GetLastCcEventsAsync(int receiptNo, CancellationToken cancellationToken = default);

    /// <summary>Register Carta de Correção at SEFAZ and persist in CC_NFE (sales only).</summary>
    Task<SendCceResult> SendCceAsync(SendCceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Report of all receipts in date range (sales TYPE='P' and manual TYPE='O' with INOUT).</summary>
    Task<IReadOnlyList<ReceiptReportRowDto>> GetReceiptReportAsync(DateTime firstDate, DateTime lastDate, CancellationToken cancellationToken = default);

    /// <summary>Generate zip of all DFE*.xml and DFE*.pdf from order folders whose LastWriteTime is in the given month/year. Returns filename (e.g. NFE_5_2011.zip).</summary>
    Task<string> GenerateZipAsync(byte month, int year, CancellationToken cancellationToken = default);

    /// <summary>List existing zip filenames in the NFe download folder.</summary>
    Task<IReadOnlyList<string>> ListZipFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>Consulta status do serviço NFe na SEFAZ (consStatServ/retConsStatServ).</summary>
    Task<ServiceStatusResultDto> GetServiceStatusAsync(CancellationToken cancellationToken = default);

    // --- Emissão em Lote (Story 10.8) ---

    /// <summary>Rows for schedule grid (orders eligible for batch NFe).</summary>
    Task<IReadOnlyList<ScheduleRowDto>> GetCurrentSchedulesAsync(CancellationToken cancellationToken = default);

    /// <summary>True if SYS_CONTROL NF_SCHEDULE = '1' (Lambda running).</summary>
    Task<bool> GetNfScheduleFlagAsync(CancellationToken cancellationToken = default);

    /// <summary>Set SYS_CONTROL NF_SCHEDULE to '1' or '0'.</summary>
    Task SetNfScheduleFlagAsync(bool value, CancellationToken cancellationToken = default);

    /// <summary>Save schedule batch: NF_SCHEDULE CRUD + ORDER.DISCOUNT + set flag=1. Does not publish SNS.</summary>
    Task SaveScheduleBatchAsync(IReadOnlyList<ScheduleItemInput> items, CancellationToken cancellationToken = default);

    /// <summary>Publish "NF" to AWS SNS topic. Returns true if published, false if topic not configured or publish failed.</summary>
    Task<bool> PublishNfScheduleSnsAsync(CancellationToken cancellationToken = default);

    /// <summary>Emit NFe for schedule (Lambda callback). Does not update NF_SCHEDULE. Returns DTO with RESULT_CODE, RESULT_MESSAGE, PDF_FILE_NAME, XML_FILE_NAME.</summary>
    Task<NFScheduleResultDto> EmitNfeForScheduleAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Set SYS_CONTROL NF_SCHEDULE = '0' (release after Lambda finishes).</summary>
    Task ReleaseNfScheduleAsync(CancellationToken cancellationToken = default);
}
