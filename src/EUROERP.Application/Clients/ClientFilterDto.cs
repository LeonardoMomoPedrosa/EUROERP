namespace EUROERP.Application.Clients;

/// <summary>Filter criteria for client list.</summary>
public class ClientFilterDto
{
    public string? Name { get; set; }
    /// <summary>CNPJ or CPF (digits only for backend).</summary>
    public string? Cnpjpf { get; set; }
    /// <summary>Optional state (STATE.PKId) filter.</summary>
    public int? StateId { get; set; }
}
