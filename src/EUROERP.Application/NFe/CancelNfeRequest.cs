using System.ComponentModel.DataAnnotations;

namespace EUROERP.Application.NFe;

public class CancelNfeRequest
{
    [Required(ErrorMessage = "Informe o número da nota.")]
    [Range(1, 10000000, ErrorMessage = "Número da nota inválido.")]
    public int ReceiptNo { get; set; }

    [Required]
    public DateTime CancelDate { get; set; }

    /// <summary>Justification for cancel (15–255 characters).</summary>
    [Required(ErrorMessage = "Digite o motivo.")]
    [MinLength(15, ErrorMessage = "O motivo deve ter no mínimo 15 caracteres.")]
    [MaxLength(255, ErrorMessage = "O motivo deve ter no máximo 255 caracteres.")]
    public string Justification { get; set; } = "";
}
