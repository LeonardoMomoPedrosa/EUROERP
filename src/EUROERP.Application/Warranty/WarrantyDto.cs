namespace EUROERP.Application.Warranty;

public class WarrantyDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int? OrderId { get; set; }
    public string? SupNf { get; set; }
    public string? EbNf { get; set; }
    public string? EbCert { get; set; }
    public string? SupCert { get; set; }
    public string? Model { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string? ChassiNo { get; set; }
    public string? BodyNo { get; set; }
    public string PlateNo { get; set; } = string.Empty;
    public string? SerialNo { get; set; }
    public DateTime InstallationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string? InstalledBy { get; set; }
    public string? PartsUsed { get; set; }
    public string? Memo { get; set; }
    public DateTime SysCreationDate { get; set; }
    public string UserId { get; set; } = string.Empty;
}
