namespace EUROERP.Application.ReferenceData;

public class FiscalClassDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public decimal? Ipi { get; set; }
    public bool Icmsst { get; set; }
    public string Name { get; set; } = string.Empty;
}
