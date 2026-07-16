using System.Text.Json;

namespace EUROERP.Infrastructure.NFe;

public interface INfeCertificateActiveConfigStore
{
    NfeActiveCertificateConfig? TryRead();
    Task SaveAsync(NfeActiveCertificateConfig config, CancellationToken cancellationToken = default);
}

public sealed class NfeActiveCertificateConfig
{
    public string CertPath { get; set; } = string.Empty;
    public string CertPassword { get; set; } = string.Empty;
}

public class NfeCertificateActiveConfigStore : INfeCertificateActiveConfigStore
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public NfeCertificateActiveConfigStore(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public NfeActiveCertificateConfig? TryRead()
    {
        var filePath = ResolveFilePath();
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<NfeActiveCertificateConfig>(json);
    }

    public async Task SaveAsync(NfeActiveCertificateConfig config, CancellationToken cancellationToken = default)
    {
        var filePath = ResolveFilePath();
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
    }

    private string ResolveFilePath()
    {
        var configured = _configuration["NFe:ActiveCertificateConfigPath"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var nfeXmlPath = _configuration["NFe:NfeXmlPath"];
        if (!string.IsNullOrWhiteSpace(nfeXmlPath))
            return Path.Combine(nfeXmlPath, "certificates", "active-certificate.json");

        return Path.Combine(AppContext.BaseDirectory, "certificates", "active-certificate.json");
    }
}

