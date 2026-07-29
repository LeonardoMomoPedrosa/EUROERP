namespace EUROERP.Application.AccountsPayable;

public interface IBillsToPayPaymentService
{
    Task<IReadOnlyList<PaymentRowDto>> GetPaymentsAsync(int billId, byte termNo, CancellationToken cancellationToken = default);
    Task RegisterPaymentAsync(int billId, byte termNo, decimal amount, DateTime paymentDate, string? memo, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task DeletePaymentAsync(int paymentPkId, CancellationToken cancellationToken = default);
}
