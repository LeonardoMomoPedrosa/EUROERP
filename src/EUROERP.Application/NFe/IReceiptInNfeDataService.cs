namespace EUROERP.Application.NFe;

/// <summary>Eurobus-only NFe Outras (RECEIPT_IN_DATA). Not in ERPCOM3/Aquanimal.</summary>
public interface IReceiptInNfeDataService
{
    Task<ReceiptInNfeDataDto?> GetByReceiptNoAsync(int receiptNo, byte version, CancellationToken cancellationToken = default);
    Task<int?> ResolveReceiptNoByInternalReceiptAsync(int internalReceipt, CancellationToken cancellationToken = default);
    Task<SaveReceiptInNfeResult> SaveV1Async(SaveReceiptInNfeV1Request request, CancellationToken cancellationToken = default);
    Task<SaveReceiptInNfeResult> SaveV2Async(SaveReceiptInNfeV2Request request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceiptInDetailDto>> GetDetailsAsync(int receiptNo, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> AddDetailAsync(AddReceiptInDetailRequest request, CancellationToken cancellationToken = default);
    Task DeleteDetailAsync(int receiptNo, string productCode, CancellationToken cancellationToken = default);
}
