namespace ServicoFaturamento.Api.Contratos;

public sealed class CriarNotaFiscalRequisicao
{
    public IReadOnlyCollection<CriarItemNotaFiscalRequisicao> Itens { get; init; }
        = [];
}
