using System.Security.Cryptography.X509Certificates;
using EUROERP.Application.NFe;
using Microsoft.Extensions.Configuration;

namespace EUROERP.Infrastructure.NFe;

public class NfeCertificateAdminService : INfeCertificateAdminService
{
    private const int DefaultMaxUploadBytes = 5 * 1024 * 1024; // 5 MB
    private readonly IConfiguration _configuration;
    private readonly INfeCertificateActiveConfigStore _activeConfigStore;

    public NfeCertificateAdminService(IConfiguration configuration, INfeCertificateActiveConfigStore activeConfigStore)
    {
        _configuration = configuration;
        _activeConfigStore = activeConfigStore;
    }

    public async Task<NfeCertificateUploadResultDto> ValidateAndStoreAsync(
        byte[] pfxContent,
        string originalFileName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (pfxContent.Length == 0)
            return Fail("Arquivo vazio.");
        if (string.IsNullOrWhiteSpace(password))
            return Fail("Senha do certificado é obrigatória.");
        if (!string.Equals(Path.GetExtension(originalFileName), ".pfx", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Path.GetExtension(originalFileName), ".p12", StringComparison.OrdinalIgnoreCase))
            return Fail("Arquivo inválido. Selecione um certificado .pfx.");

        var maxBytes = _configuration.GetValue<int?>("NFe:CertificateMaxUploadBytes") ?? DefaultMaxUploadBytes;
        if (pfxContent.Length > maxBytes)
            return Fail($"Arquivo maior que o permitido ({maxBytes / 1024 / 1024} MB).");

        X509Certificate2 cert;
        try
        {
            cert = new X509Certificate2(
                pfxContent,
                password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
        }
        catch
        {
            return Fail("Não foi possível abrir o certificado. Verifique o arquivo e a senha.");
        }

        if (!cert.HasPrivateKey)
            return Fail("Certificado inválido: chave privada não encontrada.");

        var storeDir = ResolveCertificateStorePath();
        Directory.CreateDirectory(storeDir);

        var safeTimestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var extension = string.Equals(Path.GetExtension(originalFileName), ".p12", StringComparison.OrdinalIgnoreCase) ? ".p12" : ".pfx";
        var fileName = $"nfe-cert-{safeTimestamp}{extension}";
        var fullPath = Path.Combine(storeDir, fileName);

        await File.WriteAllBytesAsync(fullPath, pfxContent, cancellationToken).ConfigureAwait(false);

        return new NfeCertificateUploadResultDto
        {
            Success = true,
            Message = "Certificado validado e salvo com sucesso.",
            StoredPath = fullPath,
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            Thumbprint = cert.Thumbprint,
            NotBefore = cert.NotBefore,
            NotAfter = cert.NotAfter
        };
    }

    public async Task<NfeCertificateUploadResultDto> ActivateStoredCertificateAsync(
        string storedPath,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return Fail("Caminho do certificado salvo é obrigatório.");
        if (!File.Exists(storedPath))
            return Fail("Arquivo de certificado não encontrado para ativação.");
        if (string.IsNullOrWhiteSpace(password))
            return Fail("Senha do certificado é obrigatória.");

        X509Certificate2 cert;
        try
        {
            cert = new X509Certificate2(
                storedPath,
                password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
        }
        catch
        {
            return Fail("Não foi possível ativar o certificado. Verifique a senha.");
        }

        if (!cert.HasPrivateKey)
            return Fail("Certificado inválido: chave privada não encontrada.");

        await _activeConfigStore.SaveAsync(new NfeActiveCertificateConfig
        {
            CertPath = storedPath,
            CertPassword = password
        }, cancellationToken).ConfigureAwait(false);

        return new NfeCertificateUploadResultDto
        {
            Success = true,
            Message = "Certificado ativado com sucesso.",
            StoredPath = storedPath,
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            Thumbprint = cert.Thumbprint,
            NotBefore = cert.NotBefore,
            NotAfter = cert.NotAfter
        };
    }

    private string ResolveCertificateStorePath()
    {
        var configured = _configuration["NFe:CertificateStorePath"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var configuredCertPath = _configuration["NFe:Cert"];
        if (!string.IsNullOrWhiteSpace(configuredCertPath))
        {
            var certDir = Path.GetDirectoryName(configuredCertPath);
            if (!string.IsNullOrWhiteSpace(certDir))
                return certDir;
        }

        var nfeXmlPath = _configuration["NFe:NfeXmlPath"];
        if (!string.IsNullOrWhiteSpace(nfeXmlPath))
            return Path.Combine(nfeXmlPath, "certificates");

        return Path.Combine(AppContext.BaseDirectory, "certificates");
    }

    private static NfeCertificateUploadResultDto Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}

