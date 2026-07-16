using System.Security.Cryptography.X509Certificates;

namespace EUROERP.Infrastructure.NFe;

public interface INfeCertificateProvider
{
    X509Certificate2 GetCertificate();
}

public class NfeCertificateProvider : INfeCertificateProvider
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly INfeCertificateActiveConfigStore _activeConfigStore;
    private X509Certificate2? _certificate;
    private string? _loadedCertPath;
    private string? _loadedCertPassword;
    private readonly object _sync = new();

    public NfeCertificateProvider(
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        INfeCertificateActiveConfigStore activeConfigStore)
    {
        _configuration = configuration;
        _activeConfigStore = activeConfigStore;
    }

    public X509Certificate2 GetCertificate()
    {
        var active = _activeConfigStore.TryRead();
        var certPath = !string.IsNullOrWhiteSpace(active?.CertPath) ? active!.CertPath : _configuration["NFe:Cert"];
        var certPassword = active?.CertPassword ?? _configuration["NFe:CertPassword"];

        if (string.IsNullOrWhiteSpace(certPath))
            throw new InvalidOperationException("NFe:Cert não configurado.");

        lock (_sync)
        {
            var sameAsLoaded = _certificate != null
                && string.Equals(_loadedCertPath, certPath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_loadedCertPassword, certPassword, StringComparison.Ordinal);
            if (sameAsLoaded)
                return _certificate!;

            try
            {
                var password = string.IsNullOrWhiteSpace(certPassword) ? "" : certPassword;
                _certificate = new X509Certificate2(certPath, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                _loadedCertPath = certPath;
                _loadedCertPassword = certPassword;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Não foi possível carregar o certificado NFe de '{certPath}'. Verifique o arquivo e a senha.", ex);
            }

            return _certificate;
        }
    }
}
