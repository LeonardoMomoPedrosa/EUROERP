namespace EUROERP.Application.SalesReports;

public interface IClientSalesRankService
{
    /// <summary>
    /// Users in role Vendas (for picker when user can see all).
    /// </summary>
    Task<IReadOnlyList<string>> GetSalesAgentsInVendasRoleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ranking for one sales agent over the last ~6 months (legacy fixed period).
    /// </summary>
    Task<ClientSalesRankDataDto> GetRankingAsync(string salesAgent, CancellationToken cancellationToken = default);
}
