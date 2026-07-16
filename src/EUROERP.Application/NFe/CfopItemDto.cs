namespace EUROERP.Application.NFe;

public class CfopItemDto
{
    public byte Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Display: "CODE - DESCRIPTION" or "PKId - CODE - DESCRIPTION".</summary>
    public string DisplayText { get; set; } = "";
}
