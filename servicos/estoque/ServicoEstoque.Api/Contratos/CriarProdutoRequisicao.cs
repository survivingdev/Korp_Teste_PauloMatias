using System.ComponentModel.DataAnnotations;

namespace ServicoEstoque.Api.Contratos;

public sealed class CriarProdutoRequisicao
{
    [Required(ErrorMessage = "O código é obrigatório.")]
    [StringLength(50, ErrorMessage = "O código deve possuir no máximo 50 caracteres.")]
    public string Codigo { get; init; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(200, ErrorMessage = "A descrição deve possuir no máximo 200 caracteres.")]
    public string Descricao { get; init; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "O saldo não pode ser negativo.")]
    public int Saldo { get; init; }
}
