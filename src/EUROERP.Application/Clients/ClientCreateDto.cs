using System.ComponentModel.DataAnnotations;
using EUROERP.Application.Validation;

namespace EUROERP.Application.Clients;

/// <summary>Client data for create form.</summary>
public class ClientCreateDto
{
    public string PersonType { get; set; } = "F";
    [Required(ErrorMessage = "CNPJ/CPF é obrigatório.")]
    public string Cnpjpf { get; set; } = string.Empty;
    public string? StateInscr { get; set; }
    public string? Contact { get; set; }
    [Required(ErrorMessage = "Nome / Razão Social é obrigatório.")]
    public string SocialName { get; set; } = string.Empty;
    public string? FantasyName { get; set; }
    public byte MarketId { get; set; } = 1;
    public int AddressCountryId { get; set; } = 1;
    public string? AddressStreet { get; set; }
    public string? AddressNumber { get; set; }
    public string? AddressComplement { get; set; }
    public string? AddressBlock { get; set; }
    [Cep(ErrorMessage = "CEP inválido. Informe 8 dígitos.")]
    public string? AddressZipCode { get; set; }
    public byte AddressStateId { get; set; }
    public short AddressCityId { get; set; }
    public string? AddressCityName { get; set; }
    public string? AddressCityIbge { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Phone3 { get; set; }
    public string? FaxNo { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public byte? PaymentMethodId { get; set; }
    public byte? PaymentMethodId2 { get; set; }
    public byte? PaymentMethodId3 { get; set; }
    public byte? AvgPayTerm { get; set; }
    public decimal? LimitAmount { get; set; }
    public byte? Birthday { get; set; }
    public byte? BirthMonth { get; set; }
    public string? BillAddressStreet { get; set; }
    public string? BillAddressBlock { get; set; }
    public int? BillAddressNumber { get; set; }
    public string? BillAddressZipCode { get; set; }
    public string BillAddressIndicator { get; set; } = "Y";
    public string? Obs { get; set; }
    public List<int> DeliverySupplierIds { get; set; } = new();
    public List<string> SalesAgentIds { get; set; } = new();
}
