namespace EUROERP.Application.ClearCustomer;

/// <summary>
/// Service for clearing (temporarily allowing) delinquent customers to place orders.
/// </summary>
public interface IClearCustomerService
{
    /// <summary>
    /// Returns true if the client has overdue AR and is within the delinquency window (not recently cleared).
    /// </summary>
    Task<bool> IsClientDelinquentAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates CLIENT.ALLOW_DELINQ and ALLOW_DELINQ_USER, granting temporary permission to place orders (24h, or 72h on Monday).
    /// </summary>
    Task AllowDelinquentClientAsync(int clientId, string userId, CancellationToken cancellationToken = default);
}
