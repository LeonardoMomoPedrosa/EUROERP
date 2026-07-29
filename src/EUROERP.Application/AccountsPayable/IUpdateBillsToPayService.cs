namespace EUROERP.Application.AccountsPayable;

public interface IUpdateBillsToPayService
{
    Task<BillToPayDetailDto?> GetDetailAsync(int billId, byte termNo, CancellationToken cancellationToken = default);
    Task UpdateDueDateAsync(int billId, byte termNo, DateTime dueDate, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task UpdateOrderDateAsync(int billId, DateTime orderDate, CancellationToken cancellationToken = default);
    Task UpdateAmountAsync(int billId, byte termNo, decimal amount, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task UpdateMemoAsync(int billId, byte termNo, string memo, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task UpdatePaymentMethodAsync(int billId, byte paymentMethodId, CancellationToken cancellationToken = default);
}
