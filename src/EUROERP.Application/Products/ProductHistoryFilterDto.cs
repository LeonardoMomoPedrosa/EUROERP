using System.ComponentModel.DataAnnotations;

namespace EUROERP.Application.Products;

/// <summary>Filter for product history: product ID and number of days.</summary>
public class ProductHistoryFilterDto
{
    [Required(ErrorMessage = "Informe o código do produto.")]
    [Range(1, 10000000, ErrorMessage = "Código do produto deve ser entre 1 e 10000000.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Informe a quantidade de dias.")]
    [Range(1, 1000, ErrorMessage = "Quantidade de dias deve ser entre 1 e 1000.")]
    public int Days { get; set; }
}
