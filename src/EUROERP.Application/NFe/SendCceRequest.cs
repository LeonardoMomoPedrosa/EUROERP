using System.ComponentModel.DataAnnotations;

namespace EUROERP.Application.NFe;

public class SendCceRequest
{
    [Required(ErrorMessage = "Informe o número da nota.")]
    [Range(1, 10000000, ErrorMessage = "Número da nota inválido.")]
    public int ReceiptNo { get; set; }

    /// <summary>Correction text (required, max 1000 chars).</summary>
    [Required(ErrorMessage = "Digite a descrição da correção.")]
    [MinLength(15, ErrorMessage = "A correção deve ter no mínimo 15 caracteres.")]
    [MaxLength(1000, ErrorMessage = "A correção deve ter no máximo 1000 caracteres.")]
    public string CorrectionText { get; set; } = "";
}
