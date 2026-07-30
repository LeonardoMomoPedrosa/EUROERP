using Microsoft.Extensions.Configuration;

namespace EUROERP.Infrastructure.NFe;

/// <summary>
/// Shared logo resolution for DANFE (NFe) and DANFSe (NFS-e) PDFs.
/// Prefer configured path when the file exists; otherwise wwwroot/images under the publish folder.
/// PNG preferred over GIF for QuestPDF.
/// </summary>
public static class DanfeLogoPathResolver
{
    public static string? Resolve(IConfiguration configuration)
    {
        var configured = configuration["NFe:DanfeLogoPath"]?.Trim();
        if (!string.IsNullOrEmpty(configured) && File.Exists(configured))
            return configured;

        var baseDir = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDir, "wwwroot", "images", "lionbw_nfe.png"),
            Path.Combine(baseDir, "wwwroot", "images", "lionbw_nfe.gif"),
            Path.Combine(baseDir, "images", "lionbw_nfe.png"),
            Path.Combine(baseDir, "images", "lionbw_nfe.gif"),
        ];
        return candidates.FirstOrDefault(File.Exists);
    }
}
