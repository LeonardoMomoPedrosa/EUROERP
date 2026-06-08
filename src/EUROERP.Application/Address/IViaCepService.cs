namespace EUROERP.Application.Address;

public interface IViaCepService
{
    Task<ViaCepResponseDto?> GetByCepAsync(string cep, CancellationToken cancellationToken = default);
}
