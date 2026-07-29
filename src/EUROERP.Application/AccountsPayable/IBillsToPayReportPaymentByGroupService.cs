namespace EUROERP.Application.AccountsPayable;

public interface IBillsToPayReportPaymentByGroupService
{
    Task<IReadOnlyList<PaymentByGroupRowDto>> GetPaymentsByGroupAsync(DateTime firstDate, DateTime lastDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentBySupplierRowDto>> GetPaymentsByGroupAndDateAsync(DateTime firstDate, DateTime lastDate, int groupId, CancellationToken cancellationToken = default);
}
