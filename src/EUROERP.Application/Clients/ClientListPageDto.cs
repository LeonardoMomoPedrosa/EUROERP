namespace EUROERP.Application.Clients;

/// <summary>One page of the client list plus the total number of matching clients.</summary>
public class ClientListPageDto
{
    public IReadOnlyList<ClientSummaryDto> Items { get; set; } = Array.Empty<ClientSummaryDto>();
    public int TotalCount { get; set; }
}
