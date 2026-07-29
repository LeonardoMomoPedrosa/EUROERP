namespace EUROERP.Application.AccountsReceivable;

public interface IUpdateBillsToReceiveService
{
    Task<BillsToReceiveDetailDto?> GetDetailAsync(int btrId, byte termNo, CancellationToken cancellationToken = default);
    Task UpdateDueDateAsync(int btrId, byte termNo, DateTime dueDate, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task UpdateAmountAsync(int btrId, byte termNo, DateTime dueDate, decimal amount, decimal paid, string memo, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task UpdatePaymentMethodAsync(int btrId, byte termNo, byte paymentMethodId, CancellationToken cancellationToken = default);
    Task<bool> HasPaymentAsync(int btrId, CancellationToken cancellationToken = default);
}
