using System.Security.Cryptography.X509Certificates;

namespace EUROERP.Infrastructure.Nfes;

public interface INfesCertificateProvider
{
    X509Certificate2 GetCertificate();
}

public class NfesCertificateProvider : INfesCertificateProvider
{
    private readonly INfesConfigProvider _configProvider;
    private X509Certificate2? _certificate;

    public NfesCertificateProvider(INfesConfigProvider configProvider)
    {
        _configProvider = configProvider;
    }

    public X509Certificate2 GetCertificate()
    {
        if (_certificate != null)
            return _certificate;

        var config = _configProvider.GetSnapshotAsync().GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(config.CertPath))
            throw new InvalidOperationException("Configure o certificado PFX em Diretoria > Configuração NFES.");

        _certificate = new X509Certificate2(
            config.CertPath,
            config.CertPassword,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
        return _certificate;
    }
}
