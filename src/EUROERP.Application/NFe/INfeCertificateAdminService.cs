namespace EUROERP.Application.NFe;

public interface INfeCertificateAdminService
{
    Task<NfeCertificateUploadResultDto> ValidateAndStoreAsync(
        byte[] pfxContent,
        string originalFileName,
        string password,
        CancellationToken cancellationToken = default);

    Task<NfeCertificateUploadResultDto> ActivateStoredCertificateAsync(
        string storedPath,
        string password,
        CancellationToken cancellationToken = default);
}

