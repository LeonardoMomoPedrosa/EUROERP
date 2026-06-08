using System.ComponentModel.DataAnnotations;

namespace EUROERP.Application.Validation;

/// <summary>Valida que o valor é um CEP válido (8 dígitos) ou vazio (para campos opcionais).</summary>
public sealed class CepAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return true;
        return CepValidator.IsValidCep(s);
    }

    public override string FormatErrorMessage(string name) =>
        ErrorMessage ?? "O campo {0} deve ser um CEP válido (8 dígitos).";
}
