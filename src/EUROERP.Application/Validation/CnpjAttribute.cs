using System.ComponentModel.DataAnnotations;

namespace EUROERP.Application.Validation;

/// <summary>Valida que o valor é um CNPJ válido ou vazio (para campos opcionais).</summary>
public sealed class CnpjAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return true;
        return CpfCnpjValidator.IsValidCnpj(s);
    }

    public override string FormatErrorMessage(string name) =>
        ErrorMessage ?? "O campo {0} deve ser um CNPJ válido (14 dígitos).";
}
