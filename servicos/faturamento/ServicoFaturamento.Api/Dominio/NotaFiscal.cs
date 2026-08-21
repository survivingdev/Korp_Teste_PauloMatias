namespace ServicoFaturamento.Api.Dominio;

public sealed class NotaFiscal
{
    public long Numero { get; set; }

    public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.Aberta;

    public DateTime CriadaEmUtc { get; set; } = DateTime.UtcNow;

    public List<ItemNotaFiscal> Itens { get; set; } = [];
}
