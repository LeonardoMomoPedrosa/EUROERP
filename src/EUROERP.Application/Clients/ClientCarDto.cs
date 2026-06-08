namespace EUROERP.Application.Clients;

public class ClientCarDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long LastKm { get; set; }
}
