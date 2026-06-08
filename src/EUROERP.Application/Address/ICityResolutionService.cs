namespace EUROERP.Application.Address;

public interface ICityResolutionService
{
    /// <summary>Finds city by state and name (accents/case insensitive) or inserts it. Returns CITY.PKId. When inserting, if ibgeCode is null and cep is provided (8 digits), consults ViaCEP for C_MUN.</summary>
    Task<short> ResolveOrCreateCityAsync(byte stateId, string cityName, string? ibgeCode, string? cep = null, CancellationToken cancellationToken = default);
}
