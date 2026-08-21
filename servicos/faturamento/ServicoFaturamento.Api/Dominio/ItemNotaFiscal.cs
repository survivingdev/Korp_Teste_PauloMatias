namespace ServicoFaturamento.Api.Dominio;

public sealed class ItemNotaFiscal
{
    public int Id { get; set; }

    public long NotaFiscalNumero { get; set; }

    public int ProdutoId { get; set; }

    public int Quantidade { get; set; }

    public NotaFiscal NotaFiscal { get; set; } = null!;
}
