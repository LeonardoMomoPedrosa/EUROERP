namespace EUROERP.Application.NFe;

/// <summary>NFES print info resolved by NFES_NO (Story 12.3).</summary>
public class NfesPrintInfoDto
{
    public int OrderId { get; set; }
    public string NfesNo { get; set; } = "";
    public string? NfesCheckCode { get; set; }
    public string? NfesChaveAcesso { get; set; }
    public string? ClientEmail { get; set; }
    public int NfesEmailCount { get; set; }
}
