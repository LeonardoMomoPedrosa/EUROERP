using System.ComponentModel.DataAnnotations;

namespace EUROERP.Application.Validation;

/// <summary>Valida que o valor é um CPF ou CNPJ válido, ou vazio (para campos opcionais "CNPJ/CPF").</summary>
public sealed class CpfOrCnpjAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return true;
        return CpfCnpjValidator.IsValidCpfOrCnpj(s);
    }

    public override string FormatErrorMessage(string name) =>
        ErrorMessage ?? "O campo {0} deve ser um CPF (11 dígitos) ou CNPJ (14 dígitos) válido.";
}
