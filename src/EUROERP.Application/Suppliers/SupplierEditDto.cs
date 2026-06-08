using System.ComponentModel.DataAnnotations;
using EUROERP.Application.Validation;

namespace EUROERP.Application.Suppliers;

/// <summary>Supplier data for edit form.</summary>
public class SupplierEditDto
{
    public int Id { get; set; }
    public int SupplierGroupId { get; set; }
    [Required(ErrorMessage = "Razão social é obrigatória.")]
    public string SocialName { get; set; } = string.Empty;
    [CpfOrCnpj(ErrorMessage = "CNPJ/CPF inválido. Informe 11 dígitos (CPF) ou 14 dígitos (CNPJ).")]
    public string? Cnpj { get; set; }
    public string? StateInscr { get; set; }
    public string? Contact { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressNumber { get; set; }
    public string? AddressComplement { get; set; }
    public string? AddressBlock { get; set; }
    [Cep(ErrorMessage = "CEP inválido. Informe 8 dígitos.")]
    public string? AddressZipCode { get; set; }
    public byte AddressStateId { get; set; }
    public short AddressCityId { get; set; }
    /// <summary>City name (from CITY on load, or from ViaCEP). Used to resolve AddressCityId on save.</summary>
    public string? AddressCityName { get; set; }
    /// <summary>IBGE code from ViaCEP. Used when inserting new city.</summary>
    public string? AddressCityIbge { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Phone3 { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public byte? BankInfoBankId { get; set; }
    public string? BankInfoAgency { get; set; }
    public string? BankInfoAccNo { get; set; }
    public string? BankInfoName { get; set; }
    public string? SwffitCode { get; set; }
    public byte? PaymentMethodId { get; set; }
    public byte? PayTerm { get; set; }
    public string? PaymentPlan { get; set; }
    public decimal? Discount { get; set; }
    public decimal? CostTransport { get; set; }
    public byte? StockDays { get; set; }
    public string? Obs { get; set; }
    public List<int> DeliverySupplierIds { get; set; } = new();
}
