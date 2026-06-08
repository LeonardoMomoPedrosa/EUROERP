using System.Text.Json;
using EUROERP.Application.Address;

namespace EUROERP.Infrastructure.Address;

public class ViaCepService : IViaCepService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ViaCepService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://viacep.com.br/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "EUROERP/1.0");
    }

    public async Task<ViaCepResponseDto?> GetByCepAsync(string cep, CancellationToken cancellationToken = default)
    {
        var digits = new string((cep ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
            return null;

        try
        {
            var json = await _httpClient.GetStringAsync($"ws/{digits}/json/", cancellationToken);
            var result = JsonSerializer.Deserialize<ViaCepResponseDto>(json, JsonOptions);
            return result?.Erro == true ? null : result;
        }
        catch
        {
            return null;
        }
    }
}
