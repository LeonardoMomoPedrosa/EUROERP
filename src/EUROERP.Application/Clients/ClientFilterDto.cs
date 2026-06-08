namespace EUROERP.Application.Clients;

/// <summary>Filter criteria for client list.</summary>
public class ClientFilterDto
{
    public string? Name { get; set; }
    /// <summary>CNPJ or CPF (digits only for backend).</summary>
    public string? Cnpjpf { get; set; }
}
