namespace EUROERP.Application.AccountsPayable;

public interface IBillsToPayApproveService
{
    /// <summary>Bills with STATUS = U (pending approval).</summary>
    Task<IReadOnlyList<BillsToPayReportRowDto>> SearchPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Set STATUS = A for each billId+termNo pair.</summary>
    Task ApproveAsync(IReadOnlyList<(int BillId, byte TermNo)> items, CancellationToken cancellationToken = default);
}
