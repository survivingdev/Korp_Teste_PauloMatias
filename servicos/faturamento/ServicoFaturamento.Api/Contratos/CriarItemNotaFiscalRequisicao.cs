using System.ComponentModel.DataAnnotations;

namespace ServicoFaturamento.Api.Contratos;

public sealed class CriarItemNotaFiscalRequisicao
{
    [Range(1, int.MaxValue, ErrorMessage = "O produto deve possuir um identificador válido.")]
    public int ProdutoId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public int Quantidade { get; init; }
}
