namespace ServicoEstoque.Api.Aplicacao;

public sealed record ItemBaixaEstoque(
    int ProdutoId,
    int Quantidade);
