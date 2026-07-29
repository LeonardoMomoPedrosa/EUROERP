namespace EUROERP.Application.AccountsReceivable;

public interface IBillsToReceiveReceiveService
{
    Task<IReadOnlyList<ReceiveRowDto>> GetReceivesAsync(int btrId, byte termNo, CancellationToken cancellationToken = default);
    Task RegisterReceiveAsync(int btrId, byte termNo, decimal amount, string? memo, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task CancelReceiveAsync(int receivePkId, CancellationToken cancellationToken = default);
}
