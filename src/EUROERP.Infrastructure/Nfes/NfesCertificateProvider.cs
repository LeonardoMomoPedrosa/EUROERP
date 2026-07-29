using System.Security.Cryptography.X509Certificates;
using EUROERP.Infrastructure.NFe;

namespace EUROERP.Infrastructure.Nfes;

public interface INfesCertificateProvider
{
    X509Certificate2 GetCertificate();
}

/// <summary>
/// Uses the same active/uploaded certificate as NFe (INfeCertificateProvider).
/// Falls back to Nfes:CertPath only when NFe cert is not configured.
/// </summary>
public class NfesCertificateProvider : INfesCertificateProvider
{
    private readonly INfeCertificateProvider _nfeCertificateProvider;
    private readonly INfesConfigProvider _configProvider;
    private X509Certificate2? _fallbackCertificate;
    private string? _fallbackPath;
    private string? _fallbackPassword;
    private readonly object _sync = new();

    public NfesCertificateProvider(INfeCertificateProvider nfeCertificateProvider, INfesConfigProvider configProvider)
    {
        _nfeCertificateProvider = nfeCertificateProvider;
        _configProvider = configProvider;
    }

    public X509Certificate2 GetCertificate()
    {
        try
        {
            return _nfeCertificateProvider.GetCertificate();
        }
        catch (InvalidOperationException)
        {
            // Fall through to Nfes-specific path when NFe:Cert / active cert is missing.
        }

        var config = _configProvider.GetSnapshotAsync().GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(config.CertPath))
            throw new InvalidOperationException(
                "Certificado não configurado. Faça upload em Diretoria → Certificado, ou configure NFe:Cert / Nfes:CertPath.");

        lock (_sync)
        {
            var same = _fallbackCertificate != null
                && string.Equals(_fallbackPath, config.CertPath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_fallbackPassword, config.CertPassword, StringComparison.Ordinal);
            if (same)
                return _fallbackCertificate!;

            _fallbackCertificate = new X509Certificate2(
                config.CertPath,
                config.CertPassword ?? "",
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            _fallbackPath = config.CertPath;
            _fallbackPassword = config.CertPassword;
            return _fallbackCertificate;
        }
    }
}
