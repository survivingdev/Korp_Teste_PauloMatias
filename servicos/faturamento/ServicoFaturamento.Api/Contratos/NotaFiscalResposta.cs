namespace ServicoFaturamento.Api.Contratos;

public sealed record NotaFiscalResposta(
    long Numero,
    string Status,
    DateTime CriadaEmUtc,
    IReadOnlyCollection<ItemNotaFiscalResposta> Itens);
