namespace EUROERP.Infrastructure.Nfes;

public interface INfesEmissionBackend
{
    string ProviderKey { get; }

    Task<NfesEmissionOutcome> EmitAsync(NfesEmissionWorkItem work, CancellationToken cancellationToken);
}
