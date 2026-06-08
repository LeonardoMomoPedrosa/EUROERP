using System.Text.Json.Serialization;

namespace EUROERP.Application.Address;

/// <summary>Response from ViaCEP API (https://viacep.com.br/ws/{cep}/json/).</summary>
public class ViaCepResponseDto
{
    [JsonPropertyName("cep")]
    public string? Cep { get; set; }

    [JsonPropertyName("logradouro")]
    public string? Logradouro { get; set; }

    [JsonPropertyName("complemento")]
    public string? Complemento { get; set; }

    [JsonPropertyName("bairro")]
    public string? Bairro { get; set; }

    [JsonPropertyName("localidade")]
    public string? Localidade { get; set; }

    [JsonPropertyName("uf")]
    public string? Uf { get; set; }

    [JsonPropertyName("ibge")]
    public string? Ibge { get; set; }

    [JsonPropertyName("erro")]
    public bool Erro { get; set; }
}
