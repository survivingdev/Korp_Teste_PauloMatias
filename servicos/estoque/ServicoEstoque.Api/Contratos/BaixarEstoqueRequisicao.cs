using System.ComponentModel.DataAnnotations;

namespace ServicoEstoque.Api.Contratos;

public sealed class BaixarEstoqueRequisicao
{
    [Range(1, long.MaxValue, ErrorMessage = "O número da nota fiscal deve ser válido.")]
    public long NotaFiscalNumero { get; init; }

    [Required(ErrorMessage = "Os itens da baixa são obrigatórios.")]
    public IReadOnlyCollection<ItemBaixaEstoqueRequisicao>? Itens { get; init; }
}
