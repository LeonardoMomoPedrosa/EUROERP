namespace EUROERP.Application.ReferenceData;

public class ProductGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IgnoreOrderDisc { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int ProductClassId { get; set; }
}
