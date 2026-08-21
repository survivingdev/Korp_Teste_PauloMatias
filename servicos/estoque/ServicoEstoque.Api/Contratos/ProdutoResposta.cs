namespace ServicoEstoque.Api.Contratos;

public sealed record ProdutoResposta(
    int Id,
    string Codigo,
    string Descricao,
    int Saldo);
