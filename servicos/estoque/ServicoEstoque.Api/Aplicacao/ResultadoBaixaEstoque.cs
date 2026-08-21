namespace ServicoEstoque.Api.Aplicacao;

public enum TipoResultadoBaixaEstoque
{
    Sucesso,
    ProdutoNaoEncontrado,
    SaldoInsuficiente
}

public sealed record ResultadoBaixaEstoque(
    TipoResultadoBaixaEstoque Tipo,
    int? ProdutoId = null,
    int? SaldoDisponivel = null);
